namespace Data.Models
{

    public class PaginationResponse<T>
    {
        public int TotalCount { get; set; }
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();

        public PaginationResponse(int totalCount, IEnumerable<T> items)
        {
            TotalCount = totalCount;
            Items = items;
        }
    }
}