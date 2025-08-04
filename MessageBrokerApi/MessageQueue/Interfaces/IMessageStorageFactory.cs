namespace MessageBrokerApi.MessageQueue.Interfaces
{
    public interface IMessageStorageFactory
    {
        IMessageStorage Create();
    }
}
