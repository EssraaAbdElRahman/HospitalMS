
namespace HospitalMS.BLL.ModelVM.Pagination
{
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public string? SearchTerm { get; set; }
    }
}
