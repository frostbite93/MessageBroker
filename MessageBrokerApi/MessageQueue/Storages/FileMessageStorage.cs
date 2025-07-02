using MessageBrokerApi.Common.Configuration;
using MessageBrokerApi.MessageQueue.Interfaces;

namespace MessageBrokerApi.MessageQueue.Storages
{
    public class FileMessageStorage : IMessageStorage
    {
        private readonly string _baseDir;
        private readonly TimeSpan _timeout;
        private readonly ILogger<FileMessageStorage> _logger;

        public FileMessageStorage(IBrokerConfig config, ILogger<FileMessageStorage> logger)
        {
            _baseDir = config.BrokerDirectory;
            if (!Directory.Exists(_baseDir))
                Directory.CreateDirectory(_baseDir);

            _timeout = TimeSpan.FromSeconds(config.BrokerTimeoutSec);
            _logger = logger;
        }

        public async Task WriteRequestAsync(string key, string method, string path, string body)
        {
            var reqPath = Path.Combine(_baseDir, $"{key}.req");

            if (File.Exists(reqPath))
            {
                _logger.LogInformation("Request file already exists: {Path}", reqPath);
                return;
            }

            try
            {
                using var stream = new FileStream(reqPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                using var writer = new StreamWriter(stream);
                await writer.WriteLineAsync(method);
                await writer.WriteLineAsync(path);
                await writer.WriteAsync(body);
                _logger.LogInformation("Request file written: {Path}", reqPath);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Failed to write request file: {Path}", reqPath);
            }
        }

        public async Task<(int StatusCode, string Body)> WaitForResponseAsync(string key)
        {
            var respPath = Path.Combine(_baseDir, $"{key}.resp");

            if (File.Exists(respPath))
            {
                _logger.LogInformation("Response file already exists: {Path}", respPath);
                return await ReadResponseAsync(respPath);
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            using var watcher = new FileSystemWatcher(_baseDir)
            {
                Filter = $"{key}.resp",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true,
            };

            FileSystemEventHandler onCreated = (s, e) =>
            {
                if (e.Name == $"{key}.resp")
                {
                    _logger.LogInformation("Watcher triggered for: {Path}", e.FullPath);
                    tcs.TrySetResult(true);
                }
            };

            watcher.Created += onCreated;
            watcher.Changed += onCreated;

            using var cts = new CancellationTokenSource(_timeout);
            using (cts.Token.Register(() => tcs.TrySetCanceled()))
            {
                try
                {
                    await tcs.Task;

                    var maxAttempts = 10;
                    for (int i = 0; i < maxAttempts; i++)
                    {
                        try
                        {
                            return await ReadResponseAsync(respPath);
                        }
                        catch (IOException)
                        {
                            await Task.Delay(50); // Wait if file busy
                        }
                    }

                    throw new IOException("File detected but not accessible after multiple attempts");
                }
                catch (TaskCanceledException)
                {
                    _logger.LogError("Timeout waiting for response file: {Path}", respPath);
                    throw new TimeoutException("Timeout waiting for broker response");
                }
            }
        }

        private async Task<(int StatusCode, string Body)> ReadResponseAsync(string path)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
                using var reader = new StreamReader(stream);

                var statusLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(statusLine) || !int.TryParse(statusLine, out var code))
                {
                    _logger.LogError("Invalid or missing status code in response file: {Path}", path);
                    return (500, "Invalid status code");
                }

                var content = await reader.ReadToEndAsync();
                _logger.LogInformation("Read response: {Code}, length={Length}", code, content.Length);
                return (code, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read response file: {Path}", path);
                return (500, "Failed to read response");
            }
        }

        public void CleanUp(string key)
        {
            var reqPath = Path.Combine(_baseDir, $"{key}.req");
            var respPath = Path.Combine(_baseDir, $"{key}.resp");

            TryDelete(reqPath);
            TryDelete(respPath);
        }

        private void TryDelete(string path)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        _logger.LogInformation("Deleted file: {Path}", path);
                        return;
                    }
                }
                catch (IOException)
                {
                    _logger.LogWarning("File in use, retrying delete: {Path}", path);
                    Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete file: {Path}", path);
                    return;
                }
            }
        }
    }
}
