using Azure.Storage.Blobs;
using CsvHelper;
using CsvHelper.Configuration;
using Data;
using Data.Models;
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Backend
{
    public class ProccessVinActivities
    {
        private readonly ILogger<ProccessVinActivities> _logger;
        private readonly EvnContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ProccessVinActivities(ILogger<ProccessVinActivities> logger, EvnContext dbContext, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [Function(nameof(ReadCsvActivity))]
        public async Task<List<Vehicle>> ReadCsvActivity([ActivityTrigger] string base64Csv)
        {
            _logger.LogInformation("ReadCsvActivity: Reading and parsing CSV data.");

            _logger.LogInformation("ReadCsvActivity: Reading and parsing CSV data from provided bytes.");

            if (string.IsNullOrEmpty(base64Csv))
            {
                _logger.LogWarning("ReadCsvActivity: Received empty or null Base64 CSV string. Returning empty list.");
                return new List<Vehicle>();
            }

            byte[] csvBytes;
            try
            {
                csvBytes = Convert.FromBase64String(base64Csv);
                _logger.LogInformation($"Successfully decoded Base64 string to {csvBytes.Length} bytes.");
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
                    // Read the records
                    csv.Context.RegisterClassMap<CarMap>();
                    var allRecords = await csv.GetRecordsAsync<Vehicle>().ToListAsync();

                    result = allRecords
                                .GroupBy(r => r.Vin)
                                .Select(g => g.OrderByDescending(r => r.ModifiedDate).First()) 
                                .ToList();

                    _logger.LogInformation($"ReadCsvActivity: Parsed {result.Count} unique VIN records.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading CSV file: {ex.Message}");
                return new List<Vehicle>();
            }

            _logger.LogInformation("CSV file read successfully.");
            return result;
        }

        [Function(nameof(ProcessBatchActivity))]
        public async Task<string> ProcessBatchActivity([ActivityTrigger] List<Vehicle> vehicles)
        {
            _logger.LogInformation($"ProcessBatchActivity: Processing batch of {vehicles.Count} records.");
            List<Vehicle> recordsToSave = new List<Vehicle>();
            string batchStatus = "Success";


            var existingCars = await _dbContext.Vehicles.AsNoTracking().Where(c => vehicles.Select(r => r.Vin).Contains(c.Vin)).ToDictionaryAsync(c => c.Vin);
            var filter = await _dbContext.VehicleVariables.AsNoTracking().Select(v => v.Id).ToListAsync();
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
                await _dbContext.BulkInsertOrUpdateAsync(recordsToSave, options => options.IncludeGraph = true);

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
                            CarId = vin
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

        public sealed class CarMap : ClassMap<Vehicle>
        {
            public CarMap()
            {
                Map(m => m.Vin).Name("Vin");
                Map(m => m.DealerId).Name("DealerId");
                Map(m => m.ModifiedDate).Name("ModifiedDate");
                Map(m => m.AdditionalVehicleInfo).Ignore();
            }
        }
    }
}
