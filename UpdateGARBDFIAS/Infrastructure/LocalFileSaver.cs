using UpdateGARBDFIAS.Interface;

namespace UpdateGARBDFIAS.Infrastructure
{
    public class LocalFileSaver : IFileSaver
    {
        private readonly ILogger<LocalFileSaver> _logger;

        public LocalFileSaver(ILogger<LocalFileSaver> logger)
        {
            _logger = logger;
        }

        public async Task SaveFileAsync(string path, byte[] data, CancellationToken cancellationToken)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllBytesAsync(path, data);
            _logger.LogInformation("File saved successfully to {Path}", path);
        }
    }
}
