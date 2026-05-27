namespace HotelOrderSystem.Api.Common;

public sealed class ApiResponse<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }

    public static ApiResponse<T> Success(T data) => new()
    {
        IsSuccess = true,
        Data = data,
        ErrorMessage = null
    };

    public static ApiResponse<T> Fail(string errorMessage) => new()
    {
        IsSuccess = false,
        Data = default,
        ErrorMessage = errorMessage
    };
}

public sealed class ApiResponse
{
    public bool IsSuccess { get; init; }
    public object? Data { get; init; }
    public string? ErrorMessage { get; init; }

    public static ApiResponse Success(object? data = null) => new()
    {
        IsSuccess = true,
        Data = data,
        ErrorMessage = null
    };

    public static ApiResponse Fail(string errorMessage) => new()
    {
        IsSuccess = false,
        Data = null,
        ErrorMessage = errorMessage
    };
}
