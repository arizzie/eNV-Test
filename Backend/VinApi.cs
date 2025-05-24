using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json; // For HttpResponseData.WriteAsJsonAsync (usually in extensions)
using System.Linq; // For common LINQ methods
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Data.Repository;
using Data.Models;

// Assume these models and repositories are defined and accessible
// using YourAppName.Models;
// using YourAppName.Repositories;


public class VinApi
{
    private readonly ILogger<VinApi> _logger;
    private readonly IVehicleRepository _vehicleRepository; // Inject the repository

    // Constructor for Dependency Injection
    public VinApi(ILogger<VinApi> logger, IVehicleRepository vehicleRepository)
    {
        _logger = logger;
        _vehicleRepository = vehicleRepository;
    }

    /// <summary>
    /// Retrieves a paginated and filterable list of vehicles.
    /// </summary>
    /// <param name="req">The HTTP request data, used to create the response.</param>
    /// <param name="pageNumber">Query parameter: The page number for pagination (1-based). Defaults to 1.</param>
    /// <param name="pageSize">Query parameter: The number of items per page. Defaults to 25.</param>
    /// <param name="sortBy">Query parameter: The column to sort by (e.g., "DealerId", "Vin", "ModifiedDate"). Defaults to "DealerId".</param>
    /// <param name="sortDirection">Query parameter: The sort direction ("ascending" or "descending"). Defaults to "ascending".</param>
    /// <param name="dealerId">Query parameter: Filter by dealer ID (partial match).</param>
    /// <param name="modifiedDate">Query parameter: Filter vehicles with ModifiedDate *after* this date (format: MM/dd/yyyy).</param>
    /// <returns>A JSON response containing total count and a list of vehicles.</returns>
    [Function("GetVins")]
    public async Task<HttpResponseData> GetVins(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "vins")] HttpRequestData req,
        // Use [FromQuery] for explicit binding, though implicit binding from HttpRequestData.Query also works
        [FromQuery(Name = "pageNumber")] int? pageNumber,
        [FromQuery(Name = "pageSize")] int? pageSize,
        [FromQuery(Name = "sort")] string? sortBy,
        [FromQuery(Name = "direction")] string? sortDirection,
        [FromQuery(Name = "dealerId")] string? dealerId,
        [FromQuery(Name = "modifiedDate")] string? modifiedDate)
    {
        _logger.LogInformation("GetVins function received a request.");

        // Create the query object from the parsed request parameters
        var query = new GetVinsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection,
            DealerId = dealerId,
            ModifiedDate = modifiedDate
        };

        try
        {
            // Delegate the data fetching to the repository
            var paginatedResponse = await _vehicleRepository.GetVehiclesAsync(query);

            // Construct and return the HTTP response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(paginatedResponse); // Serialize the PagedResult to JSON
            return response;
        }
        catch (Exception ex)
        {
            // Log the full exception for debugging
            _logger.LogError(ex, "Error fetching VINs: {Message}", ex.Message);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("An unexpected error occurred while fetching VINs. Please try again later.");
            return errorResponse;
        }
    }

    /// <summary>
    /// Retrieves detailed vehicle data for a specific VIN.
    /// </summary>
    /// <param name="req">The HTTP request data, used to create the response.</param>
    /// <param name="vin">Route parameter: The VIN to retrieve data for.</param>
    /// <returns>A JSON response containing the vehicle data or a 404 Not Found if VIN is not found.</returns>
    [Function("GetVehicleDataFromVin")]
    public async Task<HttpResponseData> GetVehicleDataFromVin(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "vins/{vin}")] HttpRequestData req,
        string vin) // 'vin' is directly bound from the route segment
    {
        _logger.LogInformation($"GetVehicleDataFromVin function received a request for VIN: {vin}");

        if (string.IsNullOrWhiteSpace(vin))
        {
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequestResponse.WriteStringAsync("VIN is required.");
            return badRequestResponse;
        }

        try
        {
            // Delegate data fetching to the repository
            var vehicle = await _vehicleRepository.GetVehicleByVinAsync(vin);

            if (vehicle == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Vehicle with VIN '{vin}' not found.");
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(vehicle); // Serialize the Vehicle object to JSON
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching vehicle data for VIN '{Vin}': {Message}", vin, ex.Message);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("An unexpected error occurred while fetching vehicle data.");
            return errorResponse;
        }
    }
}