using System.Linq.Dynamic.Core;

namespace Data.Models
{

    public class PaginationResponse
    {
        public int TotalCount { get; set; }
        public List<Vehicle> Items { get; set; } = new List<Vehicle>();

        public PaginationResponse(int totalCount, List<Vehicle> items)
        {
            TotalCount = totalCount;
            Items = items;
        }
    }
}