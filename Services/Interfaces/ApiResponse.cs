namespace CinemaBooking.API.Services.Interfaces
{
    /// <summary>
    /// Standardized API Response structure for Enterprise apps.
    /// </summary>
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public T Data { get; set; } = default!;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// API Response static helpers.
    /// </summary>
    public static class ApiResponse
    {
        public static ApiResponse<T> Success<T>(T data, string message = "")
        {
            return new ApiResponse<T> { IsSuccess = true, Data = data, Message = message };
        }

        public static ApiResponse<T> Fail<T>(string message)
        {
            return new ApiResponse<T> { IsSuccess = false, Message = message };
        }
    }
}
