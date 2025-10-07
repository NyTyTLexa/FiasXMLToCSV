namespace FiasXMLToCSV.Server.Models;
public class FiasDownloadOptions
{
    public string Url { get; set; } = string.Empty;
    public string DownloadPath { get; set; } = "Downloads";
    public int MaxRetries { get; set; } = 5;
    public int RetryDelayMs { get; set; } = 2000;
}
