namespace FiasXMLToCSV.Server.Models;

public class SchemaColumn
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public bool IsRequired { get; set; }
    public bool IsAttribute { get; set; }
}

