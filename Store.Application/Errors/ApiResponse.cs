namespace Store.API.Errors
{
    public class ApiResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public ApiResponse(int statusCode, string? message = null)
        {
            StatusCode = statusCode;
            Message = message ?? GetDefaultMessageForStatusCode(statusCode);
        }
        private string GetDefaultMessageForStatusCode(int statusCode)
        {
            return statusCode switch
            {
                400 => "Bad Request,InValid Or Missing Data",
                401 => "Invalid credentials or unauthorized access",
                404 => "Resource Not Found",
                500 => "Internal Server Error, please try again later",
                _ => "An unexpected error occurred"
            };
        }
    }
}
