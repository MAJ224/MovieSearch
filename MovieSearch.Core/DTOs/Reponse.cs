using MovieSearchCore.Enums;

namespace MovieSearchCore.DTOs.Responses
{
    public class Response<T>(T? data = default, ResponseType messageType = ResponseType.Success, string message = "Ok")
    {
        public T? Data { get; set; } = data;
        public ResponseType ResponseType { get; set; } = messageType;
        public string Message { get; set; } = message;
    }
}
