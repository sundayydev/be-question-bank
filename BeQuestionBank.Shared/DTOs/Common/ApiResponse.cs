namespace BeQuestionBank.Shared.DTOs.Common
{
    public class ApiResponse<T>
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public T? Data { get; set; }
        public object? Errors { get; set; }

        public bool Success => StatusCode >= 200 && StatusCode < 300;

        public ApiResponse(int statusCode, string message, T? data = default, object? errors = null)
        {
            StatusCode = statusCode;
            Message = message;
            Data = data;
            Errors = errors;
        }
    }
}