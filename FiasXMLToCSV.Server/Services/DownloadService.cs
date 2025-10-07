using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using FiasXMLToCSV.Server.Interface;
using FiasXMLToCSV.Server.Models;

namespace FiasXMLToCSV.Server.Services;
public class DownloadService
{
    private readonly IFileDownloader _downloader;
    private readonly IFileSaver _saver;
    private readonly IArchiveExtractor _extractor;
    private readonly ILogger<DownloadService> _logger;
    private readonly FiasDownloadOptions _options;

    public DownloadService(
        IFileDownloader downloader,
        IFileSaver saver,
        IArchiveExtractor extractor,
        ILogger<DownloadService> logger,
        IOptions<FiasDownloadOptions> options)
    {
        _downloader = downloader;
        _saver = saver;
        _extractor = extractor;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<DownloadResult> DownloadSaveAndExtractAsync(
        string url,
        string zipPath,
        string extractPath,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting FIAS delta download workflow from {Url}", url);

            // Ensure directories exist
            var zipDir = Path.GetDirectoryName(zipPath);
            if (!string.IsNullOrEmpty(zipDir))
            {
                Directory.CreateDirectory(zipDir);
            }
            Directory.CreateDirectory(extractPath);

            // Download
            var data = await _downloader.DownloadFileAsync(url, cancellationToken);

            // Save
            await _saver.SaveFileAsync(zipPath, data, cancellationToken);

            // Extract
            await _extractor.ExtractAsync(zipPath, extractPath, cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "FIAS delta successfully downloaded and extracted in {Duration:g}",
                duration);

            return new DownloadResult
            {
                Success = true,
                ZipPath = zipPath,
                ExtractPath = extractPath,
                FileSizeBytes = data.Length,
                Duration = duration
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Download operation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in download or extraction process");
            return new DownloadResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

