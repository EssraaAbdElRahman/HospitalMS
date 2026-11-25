namespace HospitalMS.BLL.ModelVM.Pagination
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = [];

        public int CurrentPage { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }
        public string? SearchTerm { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
