using Microsoft.AspNetCore.Mvc;
using UpdateGARBDFIAS.Interface;

namespace UpdateGARBDFIAS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConversionController : ControllerBase
    {
        private readonly IXmlToCsvConverter _converter;
        private readonly ILogger<ConversionController> _logger;

        public ConversionController(
            IXmlToCsvConverter converter,
            ILogger<ConversionController> logger)
        {
            _converter = converter;
            _logger = logger;
        }

        [HttpPost("xml-to-csv")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConvertXmlToCsvAsync(
            [FromQuery] string xmlPath,
            [FromQuery] string csvPath,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(xmlPath) || string.IsNullOrWhiteSpace(csvPath))
            {
                return BadRequest(new { message = "xmlPath and csvPath are required" });
            }

            try
            {
                await _converter.ConvertXmlToCsvAsync(xmlPath, csvPath, cancellationToken);

                return Ok(new
                {
                    message = "Conversion completed successfully",
                    xmlFile = xmlPath,
                    csvFile = csvPath
                });
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting XML to CSV");
                return StatusCode(500, new { message = "Conversion failed", error = ex.Message });
            }
        }

        [HttpPost("directory")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConvertDirectoryAsync(
            [FromQuery] string xmlDirectory,
            [FromQuery] string csvDirectory,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(xmlDirectory) || string.IsNullOrWhiteSpace(csvDirectory))
            {
                return BadRequest(new { message = "xmlDirectory and csvDirectory are required" });
            }

            try
            {
                await _converter.ConvertDirectoryAsync(xmlDirectory, csvDirectory, cancellationToken);

                return Ok(new
                {
                    message = "Directory conversion completed",
                    xmlDirectory,
                    csvDirectory
                });
            }
            catch (DirectoryNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting directory");
                return StatusCode(500, new { message = "Conversion failed", error = ex.Message });
            }
        }

        [HttpPost("fias-complete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConvertFiasCompleteAsync(CancellationToken cancellationToken)
        {
            const string extractPath = "Downloads/gar_delta_xml";
            const string csvPath = "Downloads/csv";

            try
            {
                _logger.LogInformation("Starting complete FIAS XML to CSV conversion");

                await _converter.ConvertDirectoryAsync(extractPath, csvPath, cancellationToken);

                return Ok(new
                {
                    message = "FIAS XML files converted to CSV successfully",
                    csvDirectory = csvPath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in complete FIAS conversion");
                return StatusCode(500, new { message = "Conversion failed", error = ex.Message });
            }
        }
    }
}
