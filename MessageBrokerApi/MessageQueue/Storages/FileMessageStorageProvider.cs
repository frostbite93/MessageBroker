using MessageBrokerApi.Common.Configuration;
using MessageBrokerApi.MessageQueue.Interfaces;
using System.Text;

namespace MessageBrokerApi.MessageQueue.Storages
{
    public class FileMessageStorageProvider : IMessageStorageProvider
    {
        private readonly string _baseDir;
        private readonly string _backendUrl;
        private readonly string _incorrectFilesDir;
        private readonly ILogger<FileMessageStorageProvider> _logger;

        public FileMessageStorageProvider(IBrokerConfig config, ILogger<FileMessageStorageProvider> logger)
        {
            _baseDir = config.BrokerDirectory;
            _backendUrl = config.BrokerBackendUrl;
            _incorrectFilesDir = config.BrokerIncorrectFilesDirectory;
            _logger = logger;

            if (!Directory.Exists(_baseDir))
                Directory.CreateDirectory(_baseDir);

            if (!Directory.Exists(_incorrectFilesDir))
                Directory.CreateDirectory(_incorrectFilesDir);
        }

        public Task<IEnumerable<string>> GetRequestsAsync()
        {
            var reqFiles = Directory.GetFiles(_baseDir, "*.req");
            var respFiles = Directory.GetFiles(_baseDir, "*.resp");
            var respFileNames = new HashSet<string>(respFiles.Select(f => Path.GetFileNameWithoutExtension(f)));

            // Возвращаем имена только тех файлов, для которых нет файла-ответа
            var fileNames = reqFiles.Where(f => !respFileNames.Contains(Path.GetFileNameWithoutExtension(f))).Select(f => Path.GetFileNameWithoutExtension(f));
            return Task.FromResult(fileNames);
        }

        public async Task<(string? Method, string? Path, string? Body)> ReadRequestAsync(string fileName)
        {
            var requestPath = Path.Combine(_baseDir, $"{fileName}.req");
            if (!File.Exists(requestPath))
                return (null, null, null);

            using var stream = new FileStream(requestPath, FileMode.Open, FileAccess.Read, FileShare.None);
            using var reader = new StreamReader(stream);

            var lines = new List<string>();
            while (!reader.EndOfStream)
                lines.Add(await reader.ReadLineAsync().ConfigureAwait(false) ?? "");

            if (lines.Count < 2)
            {
                await MoveToIncorrectAsync(requestPath);
                return (null, null, null);
            }

            var method = lines[0];
            var path = _backendUrl + lines[1];
            var body = string.Join('\n', lines.Skip(2));

            return (method, path, body);
        }

        public async Task WriteResponseAsync(string fileName, HttpResponseMessage response)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var finalContent = ((int)response.StatusCode) + Environment.NewLine + responseBody;
            var respPath = Path.Combine(_baseDir, $"{fileName}.resp");

            try
            {
                _logger.LogInformation("Writing response directly to: {Path}", respPath);
                using (var stream = new FileStream(respPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    await writer.WriteAsync(finalContent);
                }
                _logger.LogInformation("Processed and wrote response for: {Key}", fileName);
            }
            catch (IOException ioEx)
            {
                _logger.LogWarning(ioEx, "IO error while processing {Key}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error while processing {Key}", fileName);
                await MoveToIncorrectAsync(fileName);
            }
        }

        private Task MoveToIncorrectAsync(string fileName)
        {
            var reqPath = Path.Combine(_baseDir, $"{fileName}.req");
            try
            {
                var targetPath = Path.Combine(_incorrectFilesDir, $"{fileName}.req");
                File.Move(reqPath, targetPath, overwrite: true);
                _logger.LogWarning("Moved incorrect file to: {IncorrectPath}", targetPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to move incorrect file: {Path}", reqPath);
            }
            return Task.CompletedTask;
        }
    }
}