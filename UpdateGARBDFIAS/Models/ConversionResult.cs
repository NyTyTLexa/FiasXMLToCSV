namespace UpdateGARBDFIAS.Models;
public class ConversionResult
{
    public string XmlFile { get; set; } = string.Empty;
    public string? CsvFile { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

