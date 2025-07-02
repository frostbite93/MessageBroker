using MessageBrokerApi.Common.Configuration;

namespace MessageBrokerApi.MessageQueue.Services
{
    public class MessageBrokerCleanupService(ILogger<MessageBrokerCleanupService> logger, IBrokerConfig config) : BackgroundService
    {
        private readonly ILogger<MessageBrokerCleanupService> _logger = logger;
        private readonly string _baseDir = config.BrokerDirectory;
        private readonly TimeSpan _fileAgeThreshold = TimeSpan.FromMinutes(config.BrokerFileAgeThresholdMin);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CleanupService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    var files = Directory.GetFiles(_baseDir, "*.tmp")
                        .Concat(Directory.GetFiles(_baseDir, "*.resp"))
                        .Concat(Directory.GetFiles(_baseDir, "*.req"))
                        .ToList();

                    foreach (var file in files)
                    {
                        try
                        {
                            var lastWrite = File.GetLastWriteTimeUtc(file);
                            if (now - lastWrite > _fileAgeThreshold)
                            {
                                File.Delete(file);
                                _logger.LogInformation($"[Cleanup] Deleted stale file: {file}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"[Cleanup] Failed to delete file: {file}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Cleanup] Error during cleanup");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("CleanupService stopped.");
        }
    }
}