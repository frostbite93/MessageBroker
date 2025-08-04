namespace MessageBrokerApi.Common.Messages
{
    public class ResponseMessage
    {
        public string Key { get; set; } = default!;
        public int StatusCode { get; set; }
        public string Body { get; set; } = default!;
    }
}
