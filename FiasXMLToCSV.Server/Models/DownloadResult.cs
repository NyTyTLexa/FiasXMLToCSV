namespace FiasXMLToCSV.Server.Models;
public class DownloadResult
{
    public bool Success { get; set; }
    public string? ZipPath { get; set; }
    public string? ExtractPath { get; set; }
    public long FileSizeBytes { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
}
