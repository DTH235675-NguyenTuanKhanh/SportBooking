// File: Services/Interfaces/IServiceResult.cs
namespace SportBooking.Services.Interfaces;

/// <summary>
/// Kết quả chung của các thao tác service.
/// Giúp Controller xử lý thành công/thất bại mà không cần dùng exception cho luồng nghiệp vụ.
/// </summary>
public class ServiceResult
{
    public bool Success { get; init; }

    /// <summary>Thông báo lỗi hoặc thông báo thành công hiển thị cho người dùng (tiếng Việt).</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Danh sách lỗi chi tiết theo từng trường (dùng để hiển thị cạnh input).</summary>
    public IDictionary<string, string[]> Errors { get; init; } = new Dictionary<string, string[]>();

    public static ServiceResult Ok(string message = "") => new()
    {
        Success = true,
        Message = message
    };

    public static ServiceResult Fail(string message) => new()
    {
        Success = false,
        Message = message
    };

    public static ServiceResult Fail(IDictionary<string, string[]> errors, string message = "Dữ liệu không hợp lệ.")
        => new()
        {
            Success = false,
            Message = message,
            Errors = errors
        };
}

/// <summary>
/// Kết quả service kèm dữ liệu trả về.
/// </summary>
/// <typeparam name="T">Kiểu dữ liệu trả về.</typeparam>
public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; init; }

    public static ServiceResult<T> Ok(T data, string message = "") => new()
    {
        Success = true,
        Message = message,
        Data = data
    };

    public static new ServiceResult<T> Fail(string message) => new()
    {
        Success = false,
        Message = message
    };

    public static new ServiceResult<T> Fail(IDictionary<string, string[]> errors, string message = "Dữ liệu không hợp lệ.")
        => new()
        {
            Success = false,
            Message = message,
            Errors = errors
        };
}