using Data.Repository;
using Data;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Linq.Dynamic.Core; // This NuGet package is needed for OrderBy(string)

namespace Data.Repository
{
    public class VehicleRepository : IVehicleRepository
    {
        private readonly EvnContext _context;
        private readonly ILogger<VehicleRepository> _logger;

        public VehicleRepository(EvnContext context, ILogger<VehicleRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PaginationResponse<Vehicle>> GetVehiclesAsync(GetVinsQuery query)
        {
            // Start building the query
            // Keep it as IQueryable to apply filters/sorting dynamically
            IQueryable<Vehicle> vehiclesQuery = _context.Vehicles.AsQueryable(); 

            // Apply filtering
            if (!string.IsNullOrEmpty(query.DealerId))
            {
                vehiclesQuery = vehiclesQuery.Where(v => v.DealerId.Contains(query.DealerId));
            }

            if (!string.IsNullOrEmpty(query.ModifiedDate))
            {
                // Use TryParseExact for robust date parsing, expecting "MM/dd/yyyy"
                if (DateTime.TryParseExact(query.ModifiedDate, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    // Filter for vehicles with ModifiedDate *after* the provided date
                    vehiclesQuery = vehiclesQuery.Where(v => v.ModifiedDate.Date > date.Date);
                }
                else
                {
                    // Log a warning if the date format is invalid, but don't stop the request
                    _logger.LogWarning("Invalid 'modifiedDate' format received: '{ModifiedDate}'. Skipping date filter.", query.ModifiedDate);
                }
            }

            // Get total count BEFORE applying pagination (important for accurate total)
            var totalCount = await vehiclesQuery.CountAsync();

            // Apply dynamic sorting
            // It's good practice to validate `sortColumn` against a whitelist of allowed properties
            var sortColumn = query.SortBy;
            var validSortColumns = new[] { "DealerId", "Vin", "ModifiedDate" /* Add other sortable columns here */ };
            if (!validSortColumns.Contains(sortColumn, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Invalid sort column '{SortColumn}' provided. Defaulting to 'DealerId'.", sortColumn);
                sortColumn = "DealerId"; // Fallback to a safe, default column
            }

            var sortDirection = query.SortDirection?.ToLowerInvariant() == "descending" || query.SortDirection?.ToLowerInvariant() == "desc" ? "descending" : "ascending";

            // Use System.Linq.Dynamic.Core for string-based ordering
            vehiclesQuery = vehiclesQuery.OrderBy($"{sortColumn} {sortDirection}");


            // Apply pagination
            if (query.PageNumber.HasValue && query.PageSize.HasValue && query.PageNumber > 0 && query.PageSize > 0)
            {
                int skip = (query.PageNumber.Value - 1) * query.PageSize.Value;
                vehiclesQuery = vehiclesQuery.Skip(skip).Take(query.PageSize.Value);
            }
            else
            {
                _logger.LogWarning("Invalid pagination parameters: PageNumber={PageNumber}, PageSize={PageSize}. Skipping pagination.", query.PageNumber, query.PageSize);
            }


            // Execute the query and get the paginated items
            var items = await vehiclesQuery.ToListAsync();

            return new PaginationResponse<Vehicle>(totalCount, items);
        }

        public async Task<Vehicle?> GetVehicleByVinAsync(string vin)
        {
            // Add more robust VIN validation here if needed (e.g., regex)
            if (string.IsNullOrWhiteSpace(vin))
            {
                _logger.LogWarning("Attempted to get vehicle data with empty or null VIN.");
                return null; // Or throw a specific ArgumentException
            }

            var vehicle = await _context.Vehicles
                .Include(v => v.AdditionalVehicleInfo.OrderBy(avi => avi.VariableId))
                .ThenInclude(avi => avi.Variable)
                .FirstOrDefaultAsync(v => v.Vin == vin);

            return vehicle;
        }
    }
}