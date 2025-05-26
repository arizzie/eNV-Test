//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Azure.Functions.Worker;
//using Microsoft.Extensions.Logging;
//using Azure.Storage.Blobs;
//using CsvHelper.Configuration;
//using CsvHelper;
//using Db;
//using Microsoft.EntityFrameworkCore;
//using System.Threading.Tasks;
//using System.Net.Http.Json;
//using Db.Models;

//namespace eVN.Function;

//public class LoadVins
//{
//    private readonly ILogger<LoadVins> _logger;
//    private readonly EvnContext _context;

//    public LoadVins(ILogger<LoadVins> logger, EvnContext context)
//    {
//        _context = context;
//        _logger = logger;
//    }

//    [Authorize]
//    [Function("LoadVins")]
//    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
//    {
//        _logger.LogInformation("C# HTTP trigger function processed a request.");

//        return await ReadCsvFile();
//    }

//    private async Task<StatusCodeResult> ReadCsvFile()
//    {

//        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
//        string containerName = "evntest";
//        string blobName = "sample-vin-data.csv";

//        BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
//        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
//        BlobClient blobClient = containerClient.GetBlobClient(blobName);

//        try
//        {
//            // Download the blob as a stream
//            using (var stream = await blobClient.OpenReadAsync())
//            {
//                // Create a StreamReader to read the stream
//                using (var reader = new StreamReader(stream))
//                {
//                    // Create a CsvReader to read the CSV file
//                    var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
//                    {
//                        HasHeaderRecord = true,
//                        Delimiter = ",",
//                    };
//                    using (var csv = new CsvReader(reader, config))
//                    {
//                        // Read the records
//                        csv.Context.RegisterClassMap<CarMap>();
//                        var records = csv.GetRecords<Vehicle>().GroupBy(r => r.Vin).Select(g => g.OrderByDescending(r => r.ModifiedDate).First()).ToList();

//                        var existingCars = await _context.Vehicles.Where(c => records.Select(r => r.Vin).Contains(c.Vin)).ToDictionaryAsync(c => c.Vin);
//                        var filter = await _context.VehicleVariables.Select(v => v.Id).ToListAsync();
//                        foreach (var record in records)
//                        {
//                            // Check if the car already exists in the database
//                            if (!existingCars.ContainsKey(record.Vin))
//                            {
//                                // Add the new car record
//                                var newCar = new Vehicle
//                                {
//                                    Vin = record.Vin,
//                                    DealerId = record.DealerId,
//                                    ModifiedDate = record.ModifiedDate,
//                                    AdditionalVehicleInfo = await GetAdditionalCarInfos(record.Vin, filter)
//                                };
//                                _context.Vehicles.Add(newCar);
//                            }
//                            else if (existingCars[record.Vin].ModifiedDate < record.ModifiedDate)
//                            {
//                                // Update the existing car record
//                                var existingCar = existingCars[record.Vin];
//                                existingCar.DealerId = record.DealerId;
//                                existingCar.ModifiedDate = record.ModifiedDate;
//                                existingCar.AdditionalVehicleInfo = await GetAdditionalCarInfos(record.Vin);
//                            }
                            
//                        }
//                        // Save changes to the database
//                        await _context.SaveChangesAsync();
//                    }
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError($"Error reading CSV file: {ex.Message}");
//            return new StatusCodeResult(500);
//        }

//        _logger.LogInformation("CSV file read successfully.");
//        return new OkResult();

//    }

//    private async Task<List<AdditionalVehicleInfo>> GetAdditionalCarInfos(string vin, List<int> filter = null)
//    {
//        HttpClient client = new HttpClient();
//        var additionalInfo = new List<AdditionalVehicleInfo>();

//        // Send a GET request to the API
//        var response = await client.GetAsync($"https://vpic.nhtsa.dot.gov/api/vehicles/decodevin/{vin}?format=json");

//        // Check if the response is successful
//        if (response.IsSuccessStatusCode)
//        {
//            // Parse the response

//            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

//            if (result != null)
//            {
//                var query = result.Results.AsQueryable();

//                if (filter != null)
//                {
//                    query = query.Where(v => filter.Contains(v.VariableId));
//                }

//                additionalInfo = query.Select(r => new AdditionalVehicleInfo
//                {
//                    Value = r.Value ?? "",
//                    VariableId = r.VariableId,
//                    VehicleId = vin
//                }).ToList();

//                var vars = additionalInfo.Select(x => x.VariableId).ToList();

//                _logger.LogInformation($"Decoded VIN: {vin}");
//            }
//        }

//        // return the list of additional car info
//        return additionalInfo ?? new List<AdditionalVehicleInfo>();
//    }

//    /// <summary>
//    /// Model class for the API response
//    /// </summary>
//    private class ApiResponse
//    {
//        public class Result
//        {
//            public string Variable { get; set; }
//            public string Value { get; set; }
//            public int VariableId { get; set; }
//        }

//        public int Count { get; set; }
//        public string Message { get; set; }
//        public string SearchCriteria { get; set; }
//        public List<Result> Results { get; set; }
//    }


//    // Map class for CSV
//    // This class maps the CSV columns to the Vehicle class properties
//    public sealed class CarMap : ClassMap<Vehicle>
//    {
//        public CarMap()
//        {
//            Map(m => m.Vin).Name("Vin");
//            Map(m => m.DealerId).Name("DealerId");
//            Map(m => m.ModifiedDate).Name("ModifiedDate");
//            Map(m => m.AdditionalVehicleInfo).Ignore();
//        }
//    }
//}