using Azure.Storage.Blobs;
using Backend.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Data;
using Data.Models;
using Data.Repository;
using EFCore.BulkExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Json;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Backend
{
    public class ProccessVinActivities
    {
        private readonly ILogger<ProccessVinActivities> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IVehicleRepository _vehicleRepository;

        public ProccessVinActivities(ILogger<ProccessVinActivities> logger, IHttpClientFactory httpClientFactory, IVehicleRepository vehicleRepository)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _vehicleRepository = vehicleRepository;
        }

        [Function(nameof(ReadCsvActivity))]
        public async Task<List<Vehicle>> ReadCsvActivity([ActivityTrigger] string base64Csv)
        {
            _logger.LogInformation("ReadCsvActivity: Reading and parsing CSV data.");

            if (string.IsNullOrEmpty(base64Csv))
            {
                _logger.LogWarning("ReadCsvActivity: Received empty or null Base64 CSV string. Returning empty list.");
                return new List<Vehicle>();
            }

            byte[] csvBytes;
            try
            {
                csvBytes = Convert.FromBase64String(base64Csv);
                _logger.LogInformation($"ReadCsvActivity: Successfully decoded Base64 string to {csvBytes.Length} bytes.");
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "ReadCsvActivity: Invalid Base64 string format received.");
                throw new ArgumentException("Input string is not a valid Base64 string.", ex);
            }

            List<Vehicle> result = new List<Vehicle>();

            try
            {
                using (var stream = new MemoryStream(csvBytes))
                using (var reader = new StreamReader(stream))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Delimiter = ",",
                    PrepareHeaderForMatch = args => args.Header.ToLower(),
                }))
                {

                    _logger.LogInformation($"ReadCsvActivity: Parsed {result.Count} unique VIN records.");


                    csv.Context.RegisterClassMap<VehicleMap>(); // Register your map

                    while (csv.Read())
                    {
                        try
                        {
                            var record = csv.GetRecord<Vehicle>();
                            result.Add(record);
                            _logger.LogInformation("Successfully processed record for VIN: {Vin}", record.Vin);
                        }
                        catch (FieldValidationException ex)
                        {
                            // Log specific field validation errors
                            _logger.LogWarning(ex, "CSV Field Validation Error in row {Row}, field {Field}: {Message}. Content: '{Content}'",
                                csv.Context.Parser.Row, ex.Field, ex.Message, ex.Field);
                            // You might store these errors or skip the record, depending on your business logic
                        }
                        catch (ReaderException ex)
                        {
                            // Log general CSV parsing errors (e.g., bad row format, malformed CSV)
                            _logger.LogError(ex, "CSV Reader Error in row {Row}: {Message}",
                               csv.Context.Parser.Row, ex.Message);
                        }
                        catch (Exception ex)
                        {
                            // Catch any other unexpected exceptions during record processing
                            _logger.LogError(ex, "An unexpected error occurred while processing CSV record in row {Row}: {Message}",
                                csv.Context.Parser.Row, ex.Message);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ReadCsvActivity: Error reading CSV file: {ex.Message}");
                return new List<Vehicle>();
            }

            _logger.LogInformation("ReadCsvActivity: CSV file read successfully.");

            return result.GroupBy(r => r.Vin)
                                .Select(g => g.OrderByDescending(r => r.ModifiedDate).First())
                                .ToList();
        }

        [Function(nameof(ProcessBatchActivity))]
        public async Task<string> ProcessBatchActivity([ActivityTrigger] List<Vehicle> vehicles)
        {
            _logger.LogInformation($"ProcessBatchActivity: Processing batch of {vehicles.Count} records.");
            List<Vehicle> recordsToSave = new List<Vehicle>();
            string batchStatus = "Success";


            var existingCars = await _vehicleRepository.GetVehiclesByVinAsync(vehicles.Select(v => v.Vin));
            var filter = (await _vehicleRepository.GetVariableFilter()).Select(i => i.Id).ToList();
            foreach (var record in vehicles)
            {
                // Check if the car already exists in the database
                if (!existingCars.ContainsKey(record.Vin))
                {
                    // Add the new car record
                    var newCar = new Vehicle
                    {
                        Vin = record.Vin,
                        DealerId = record.DealerId,
                        ModifiedDate = record.ModifiedDate,
                        AdditionalVehicleInfo = await GetAdditionalCarInfos(record.Vin, filter)
                    };

                    recordsToSave.Add(newCar);
                }
                else if (existingCars[record.Vin].ModifiedDate < record.ModifiedDate)
                {
                    // Update the existing car record
                    var existingCar = existingCars[record.Vin];
                    existingCar.DealerId = record.DealerId;
                    existingCar.ModifiedDate = record.ModifiedDate;
                    existingCar.AdditionalVehicleInfo = await GetAdditionalCarInfos(record.Vin);

                    recordsToSave.Add(existingCar);
                }

            }


            //  Bulk Save to Database using EFCore.BulkExtensions
            try
            {
                await _vehicleRepository.SaveVehiclesBatchAsync(recordsToSave);

                _logger.LogInformation($"ProcessBatchActivity: Successfully saved batch of {recordsToSave.Count} records to DB.");
                return $"Batch processed ({recordsToSave.Count} records) - {batchStatus}";
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, $"Database save error for batch: {dbEx.Message}");
                return $"Batch processing failed during DB save - {dbEx.Message}";
            }
        }


        private async Task<List<AdditionalVehicleInfo>> GetAdditionalCarInfos(string vin, List<int> filter = null)
        {
            _logger.LogInformation($"GetAdditionalCarInfos: Fetching data for {vin}");
            var additionalInfo = new List<AdditionalVehicleInfo>();
            try
            {
                var client = _httpClientFactory.CreateClient("ExternalApiClient");
                var apiEndpoint = $"vehicles/decodevin/{vin}?format=json";


                // Send a GET request to the API
                var response = await client.GetAsync(apiEndpoint);

                // Check if the response is successful
                if (response.IsSuccessStatusCode)
                {
                    // Parse the response

                    var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

                    if (result != null)
                    {
                        var query = result.Results.AsQueryable();

                        if (filter != null)
                        {
                            query = query.Where(v => filter.Contains(v.VariableId));
                        }

                        additionalInfo = query.Select(r => new AdditionalVehicleInfo
                        {
                            Value = r.Value ?? "",
                            VariableId = r.VariableId,
                            VehicleId = vin
                        }).ToList();

                        var vars = additionalInfo.Select(x => x.VariableId).ToList();

                        _logger.LogInformation($"Decoded VIN: {vin}");
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, $"HTTP request error processing record {vin}: {httpEx.Message}");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"General error during API call for record {vin}: {ex.Message}");

            }

            // return the list of additional car info
            return additionalInfo ?? new List<AdditionalVehicleInfo>();
        }

        [Function("ArchiveOriginalCsvBlob")]
        public static async Task<Uri> ArchiveOriginalCsvBlob(
        [ActivityTrigger] BlobUploadActivityInput input,
        FunctionContext context) // Use FunctionContext for .NET isolated
        {
            ILogger logger = context.GetLogger("ArchiveOriginalCsvBlob");
            logger.LogInformation($"Archiving original CSV blob '{input.BlobName}' to container '{input.ContainerName}'.");

            try
            {
                // Get the connection string from application settings
                string connectionString = Environment.GetEnvironmentVariable(input.ConnectionStringName ?? "AzureWebJobsStorage");

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new ArgumentException($"Azure Storage connection string '{input.ConnectionStringName}' is not configured.");
                }

                // Create a BlobServiceClient
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(input.ContainerName);
                await containerClient.CreateIfNotExistsAsync();

                BlobClient blobClient = containerClient.GetBlobClient(input.BlobName);

                byte[] fileBytes = Convert.FromBase64String(input.FileContent);

                using (var stream = new MemoryStream(fileBytes))
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                logger.LogInformation($"Original CSV blob archived successfully. URI: {blobClient.Uri}");
                return blobClient.Uri;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error archiving original CSV blob '{input.BlobName}': {ex.Message}");
                // Re-throw the exception to signal failure to the orchestrator
                throw;
            }
        }


        /// <summary>
        /// Model class for the API response
        /// </summary>
        private class ApiResponse
        {
            public class Result
            {
                public string Variable { get; set; }
                public string Value { get; set; }
                public int VariableId { get; set; }
            }

            public int Count { get; set; }
            public string Message { get; set; }
            public string SearchCriteria { get; set; }
            public List<Result> Results { get; set; }
        }

        public class VehicleMap : ClassMap<Vehicle>
        {
            // Regex for Post-1981 (17-character, no I, O, Q)
            private static readonly Regex Post1981VinRegex =
                new Regex("^[A-HJ-NPR-Z0-9]{17}$", RegexOptions.Compiled);

            // Regex for Pre-1981 (More permissive: 11 to 17 alphanumeric characters)
            private static readonly Regex Pre1981VinRegex =
                new Regex("^[A-Z0-9]{11,17}$", RegexOptions.Compiled);

            // Regex for numeric characters only (for DealerId)
            private static readonly Regex NumericOnlyRegex =
                new Regex(@"^\d+$", RegexOptions.Compiled);

            public VehicleMap()
            {
                Map(m => m.Vin).Name("Vin").Validate(args =>
                {

                    string vin = args.Field;

                    if (string.IsNullOrWhiteSpace(vin))
                    {

                        throw new FieldValidationException(
                            context: null, 
                            field: "Vin",
                            message: "VIN cannot be empty or whitespace.");
                    }

                    if (Post1981VinRegex.IsMatch(vin))
                    {
                        return true; // Valid Post-1981 VIN
                    }

                    if (Pre1981VinRegex.IsMatch(vin))
                    {
                        return true; // Valid Pre-1981 VIN
                    }

                    throw new FieldValidationException(
                        context: null,
                        field: "Vin",
                        message: $"Invalid VIN format: '{vin}'. Must be 17 alphanumeric (no I, O, Q) or 11-17 alphanumeric (any letter/digit).");
                });

                Map(m => m.DealerId).Name("DealerId").Validate(args =>
                {
                    string dealerId = args.Field;

                    if (string.IsNullOrWhiteSpace(dealerId))
                    {
                        throw new FieldValidationException(
                            context: null, // No ValidateContext in older CsvHelper versions for this signature
                            field: "DealerId",
                            message: "DealerId cannot be empty or whitespace.");
                    }

                    if (!NumericOnlyRegex.IsMatch(dealerId))
                    {
                        throw new FieldValidationException(
                            context: null, // No ValidateContext in older CsvHelper versions for this signature
                            field: "DealerId",
                            message: $"DealerId must contain only numeric characters. Found: '{dealerId}'.");
                    }
                    return true; // Valid DealerId
                });

                Map(m => m.ModifiedDate).Name("ModifiedDate");
                Map(m => m.AdditionalVehicleInfo).Ignore();
            }
        }
    }
}