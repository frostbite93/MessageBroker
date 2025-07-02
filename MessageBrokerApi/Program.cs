using MessageBrokerApi.Backend.Interfaces;
using MessageBrokerApi.Backend.Services;
using MessageBrokerApi.Common.Configuration;
using MessageBrokerApi.Common.Hashing;
using MessageBrokerApi.MessageQueue.Interfaces;
using MessageBrokerApi.MessageQueue.Services;
using MessageBrokerApi.MessageQueue.Storages;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<IBrokerConfig, BrokerConfig>();
builder.Services.AddSingleton<IHashGenerator, MD5HashGenerator>();
builder.Services.AddSingleton<IBackendRequest, BackendRequest>();
builder.Services.AddSingleton<IMessageStorage, FileMessageStorage>();
builder.Services.AddSingleton<IMessageBroker, MessageBroker>();
builder.Services.AddSingleton<IMessageStorageProvider, FileMessageStorageProvider>();
builder.Services.AddHttpClient();

var config = builder.Configuration;
bool runMessageBrokerConsumer = config.GetValue<bool>("RunServices:RunMessageBrokerConsumer");

builder.Services.AddHostedService<MessageBrokerCleanupService>();

if (runMessageBrokerConsumer)
    builder.Services.AddHostedService<MessageBrokerConsumerService>();

var app = builder.Build();
app.MapGet("/", () => Results.Ok());
app.MapGet("/favicon.ico", () => Results.NotFound());
app.MapControllers();
app.Run();