using System.Xml.Schema;
using UpdateGARBDFIAS.Models;

namespace UpdateGARBDFIAS.Infrastructure;
public class FiasSchemaParser
{
    private readonly ILogger<FiasSchemaParser> _logger;

    public FiasSchemaParser(ILogger<FiasSchemaParser> logger)
    {
        _logger = logger;
    }

    public Dictionary<string, List<SchemaColumn>> ParseSchemas(string schemaDirectory)
    {
        var result = new Dictionary<string, List<SchemaColumn>>();

        var schemaSet = new XmlSchemaSet();
        var xsdFiles = Directory.GetFiles(schemaDirectory, "*.xsd", SearchOption.AllDirectories);

        foreach (var xsdFile in xsdFiles)
        {
            try
            {
                schemaSet.Add(null, xsdFile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load schema: {Schema}", xsdFile);
            }
        }

        schemaSet.Compile();

        foreach (XmlSchema schema in schemaSet.Schemas())
        {
            foreach (var item in schema.Items)
            {
                if (item is XmlSchemaElement element)
                {
                    var columns = ExtractColumnsFromElement(element);
                    if (columns.Any())
                    {
                        result[element.Name] = columns;
                        _logger.LogInformation(
                            "Parsed schema for {Element}: {ColumnCount} columns",
                            element.Name,
                            columns.Count);
                    }
                }
            }
        }

        return result;
    }

    private List<SchemaColumn> ExtractColumnsFromElement(XmlSchemaElement element)
    {
        var columns = new List<SchemaColumn>();

        if (element.ElementSchemaType is XmlSchemaComplexType complexType)
        {
            // Атрибуты
            foreach (var attribute in GetAttributes(complexType))
            {
                columns.Add(new SchemaColumn
                {
                    Name = attribute.Name,
                    Type = GetDataType(attribute.AttributeSchemaType),
                    IsRequired = attribute.Use == XmlSchemaUse.Required,
                    IsAttribute = true
                });
            }

            // Элементы
            if (complexType.Particle is XmlSchemaSequence sequence)
            {
                foreach (var item in sequence.Items)
                {
                    if (item is XmlSchemaElement childElement)
                    {
                        columns.Add(new SchemaColumn
                        {
                            Name = childElement.Name,
                            Type = GetDataType(childElement.ElementSchemaType),
                            IsRequired = childElement.MinOccurs > 0,
                            IsAttribute = false
                        });
                    }
                }
            }
        }

        return columns;
    }

    private IEnumerable<XmlSchemaAttribute> GetAttributes(XmlSchemaComplexType complexType)
    {
        var attributes = new List<XmlSchemaAttribute>();

        foreach (var attr in complexType.Attributes)
        {
            if (attr is XmlSchemaAttribute attribute)
            {
                attributes.Add(attribute);
            }
        }

        return attributes;
    }

    private string GetDataType(XmlSchemaType? schemaType)
    {
        if (schemaType == null)
            return "string";

        var typeName = schemaType.QualifiedName.Name.ToLowerInvariant();

        return typeName switch
        {
            "int" or "integer" or "long" => "int",
            "decimal" or "double" or "float" => "decimal",
            "boolean" or "bool" => "bool",
            "date" or "datetime" => "datetime",
            "time" => "time",
            _ => "string"
        };
    }
}

