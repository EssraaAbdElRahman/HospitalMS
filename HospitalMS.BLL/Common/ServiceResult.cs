
namespace HospitalMS.BLL.Common
{
    public class ServiceResult
    {
        public bool Succeeded { get; set; }
        public List<string> Errors { get; set; } = new();

        public static ServiceResult Success() => new() { Succeeded = true };
        public static ServiceResult Failure(params string[] errors) => new() { Succeeded = false, Errors = errors.ToList() };
        public static ServiceResult Failure(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors.ToList() };
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; set; }

        public static ServiceResult<T> Success(T data) => new() { Succeeded = true, Data = data };
        public static new ServiceResult<T> Failure(params string[] errors) => new() { Succeeded = false, Errors = errors.ToList() };
        public static new ServiceResult<T> Failure(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors.ToList() };
    } 
}
