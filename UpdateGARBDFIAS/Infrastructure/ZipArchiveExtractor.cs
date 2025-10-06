using System.IO.Compression;
using UpdateGARBDFIAS.Interface;

namespace UpdateGARBDFIAS.Infrastructure;
public class ZipArchiveExtractor : IArchiveExtractor
{
    private readonly ILogger<ZipArchiveExtractor> _logger;

    public ZipArchiveExtractor(ILogger<ZipArchiveExtractor> logger)
    {
        _logger = logger;
    }

    public async Task ExtractAsync(string archivePath, string destinationPath, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting extraction of {Archive}", archivePath);

        if (!File.Exists(archivePath))
        {
            _logger.LogError("Archive not found at {Path}", archivePath);
            throw new FileNotFoundException("Archive file not found.", archivePath);
        }

        if (Directory.Exists(destinationPath))
        {
            _logger.LogInformation("Cleaning up existing directory {Dir}", destinationPath);
            Directory.Delete(destinationPath, recursive: true);
        }

        Directory.CreateDirectory(destinationPath);

        await Task.Run(() => ZipFile.ExtractToDirectory(archivePath, destinationPath));

        _logger.LogInformation("Extraction completed to {Destination}", destinationPath);
    }
}
