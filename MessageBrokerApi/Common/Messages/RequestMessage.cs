namespace MessageBrokerApi.Common.Messages
{
    public class RequestMessage
    {
        public string Key { get; set; } = default!;
        public string Method { get; set; } = default!;
        public string Path { get; set; } = default!;
        public string Body { get; set; } = default!;
    }
}
