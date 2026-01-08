namespace VMS.API.Common.Models;

public class BaseResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static BaseResponse<T> SuccessResponse(T data, string? message = null)
    {
        return new BaseResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static BaseResponse<T> ErrorResponse(string message, List<string>? errors = null)
    {
        return new BaseResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}



















