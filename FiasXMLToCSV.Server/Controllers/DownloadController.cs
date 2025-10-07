using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using FiasXMLToCSV.Server.Models;
using FiasXMLToCSV.Server.Services;

namespace FiasXMLToCSV.Server.Controllers;


[ApiController]
[Route("api/[controller]")]
public class DownloadController : ControllerBase
{
    private readonly DownloadService _downloadService;
    private readonly ILogger<DownloadController> _logger;
    private readonly FiasDownloadOptions _options;

    public DownloadController(
        DownloadService downloadService,
        ILogger<DownloadController> logger,
        IOptions<FiasDownloadOptions> options)
    {
        _downloadService = downloadService;
        _logger = logger;
        _options = options.Value;
    }

    [HttpGet("fias")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadAndExtractFiasAsync(CancellationToken cancellationToken)
    {
        try
        {
            var zipPath = Path.Combine(_options.DownloadPath, "gar_delta_xml.zip");
            var extractPath = Path.Combine(_options.DownloadPath, "gar_delta_xml");

            _logger.LogInformation("Received request to download and extract FIAS delta XML");

            var result = await _downloadService.DownloadSaveAndExtractAsync(
                _options.Url,
                zipPath,
                extractPath,
                cancellationToken);

            if (!result.Success)
            {
                return StatusCode(500, new
                {
                    message = "Failed to download and extract FIAS data",
                    error = result.ErrorMessage
                });
            }

            return Ok(new
            {
                message = "FIAS delta XML downloaded and extracted successfully",
                zipFile = result.ZipPath,
                extractedTo = result.ExtractPath,
                fileSizeMB = Math.Round(result.FileSizeBytes / 1024.0 / 1024.0, 2),
                durationSeconds = result.Duration.TotalSeconds
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Request was cancelled");
            return StatusCode(499, new { message = "Request cancelled by client" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing FIAS download request");
            return StatusCode(500, new
            {
                message = "An unexpected error occurred",
                error = ex.Message
            });
        }
    }
}


