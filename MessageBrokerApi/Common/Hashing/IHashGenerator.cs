namespace MessageBrokerApi.Common.Hashing
{
    public interface IHashGenerator
    {
        string ComputeHash(string input);
    }
}