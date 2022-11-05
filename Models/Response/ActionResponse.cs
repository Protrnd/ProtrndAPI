namespace ProtrndWebAPI.Models.Response
{
    public class ActionResponse
    {
        public bool Successful { get; set; } = false;
        public int StatusCode { get; set; } = 400;
        public string Message { get; set; } = ActionResponseMessage.BadRequest;
        public object? Data { get; set; } = null;
    }
}
