using MessageBrokerApi.MessageQueue.Factories;
using MessageBrokerApi.MessageQueue.Interfaces;

namespace MessageBrokerApi.MessageQueue.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMessageStorage(this IServiceCollection services)
        {
            services.AddSingleton<IMessageStorageFactory, MessageStorageFactory>();
            services.AddTransient(sp => sp.GetRequiredService<IMessageStorageFactory>().Create());

            return services;
        }
    }
}
