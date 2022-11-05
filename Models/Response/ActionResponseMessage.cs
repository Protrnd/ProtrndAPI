namespace ProtrndWebAPI.Models.Response
{
    public class ActionResponseMessage
    {
        // 2xx Success
        public const string Ok = "Successful!"; // Response message for status code 200
        public const string Created = "Created!"; // Response message for status code 201
        public const string Accepted = "Request Accepted!"; // Response message for status code 202
        public const string NoContent = "No Content!"; // Response message for status code 204

        // 3xx Redirection
        public const string NotModified = "Not Modified!"; // Response message for status code 304

        // 4xx Client Error
        public const string BadRequest = "An error occurred!"; // Response message for status code 400
        public const string Unauthorized = "Unauthorized!"; // Response message for status code 401
        public const string Forbidden = "Forbidden!"; // Response message for status code 403
        public const string NotFound = "Not Found!"; // Response message for status code 404
        public const string Conflict = "Conflict!"; // Response message for status code 409

        // 5xx Server Error
        public const string ServerError = "Internal Server Error"; // Response message for status code 500 
    }
}