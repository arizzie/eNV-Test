using System.Globalization;
namespace Backend.Models
{
    public class GetVinsQuery
    {
        public int? PageNumber { get; set; } = 1; // Default to page 1
        public int? PageSize { get; set; } = 25; // Default to 25 items per page
        public string SortBy { get; set; } = "DealerId"; // Default sort column
        public string SortDirection { get; set; } = "ascending"; // Default sort direction
        public string? DealerId { get; set; } // Nullable string for filter
        public string? ModifiedDate { get; set; } // Date as string for parsing

        // Helper to parse query parameters from HttpRequestData.Query
        public static GetVinsQuery FromQuery(IReadOnlyDictionary<string, string> query)
        {
            return new GetVinsQuery
            {
                PageNumber = query.TryGetValue("pageNumber", out var pn) && int.TryParse(pn, out var pageNum) ? pageNum : (int?)null,
                PageSize = query.TryGetValue("pageSize", out var ps) && int.TryParse(ps, out var pageSize) ? pageSize : (int?)null,
                SortBy = query.TryGetValue("sort", out var s) && !string.IsNullOrWhiteSpace(s) ? s : "DealerId",
                SortDirection = query.TryGetValue("direction", out var d) && !string.IsNullOrWhiteSpace(d) ? d : "ascending",
                DealerId = query.TryGetValue("dealerId", out var di) ? di : null,
                ModifiedDate = query.TryGetValue("modifiedDate", out var md) ? md : null
            };
        }
    }
}