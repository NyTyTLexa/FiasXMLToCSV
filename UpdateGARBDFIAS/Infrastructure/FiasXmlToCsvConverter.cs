using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using UpdateGARBDFIAS.Interface;
using UpdateGARBDFIAS.Models;

namespace UpdateGARBDFIAS.Infrastructure
{
    public class FiasXmlToCsvConverter : IXmlToCsvConverter
    {
        private readonly ILogger<FiasXmlToCsvConverter> _logger;
        private readonly FiasSchemaParser _schemaParser;
        private Dictionary<string, List<SchemaColumn>>? _schemaDefinitions;

        public FiasXmlToCsvConverter(
            ILogger<FiasXmlToCsvConverter> logger,
            FiasSchemaParser schemaParser)
        {
            _logger = logger;
            _schemaParser = schemaParser;
        }

        /// <summary>
        /// Загружает XSD схемы из директории
        /// </summary>
        public void LoadSchemas(string schemaDirectory)
        {
            _logger.LogInformation("Loading XSD schemas from {SchemaDirectory}", schemaDirectory);

            _schemaDefinitions = _schemaParser.ParseSchemas(schemaDirectory);

            _logger.LogInformation(
                "Loaded {Count} schema definitions",
                _schemaDefinitions.Count);
        }

        /// <summary>
        /// Конвертирует XML файл в CSV
        /// </summary>
        public async Task ConvertXmlToCsvAsync(
            string xmlPath,
            string csvPath,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(xmlPath))
            {
                throw new FileNotFoundException($"XML file not found: {xmlPath}");
            }

            _logger.LogInformation("Converting {XmlFile} to {CsvFile}", xmlPath, csvPath);

            var startTime = DateTime.UtcNow;
            int recordCount = 0;

            try
            {
                // Определяем корневой элемент и его структуру
                var rootElementName = GetRootDataElementName(xmlPath);
                if (string.IsNullOrEmpty(rootElementName))
                {
                    _logger.LogWarning("Could not determine root element for {XmlFile}", xmlPath);
                    return;
                }

                // Получаем колонки из схемы или из первого элемента
                List<string> columns;
                if (_schemaDefinitions != null && _schemaDefinitions.ContainsKey(rootElementName))
                {
                    columns = _schemaDefinitions[rootElementName]
                        .Select(c => c.Name)
                        .ToList();
                    _logger.LogInformation(
                        "Using XSD schema for '{Element}' with {Count} columns",
                        rootElementName,
                        columns.Count);
                }
                else
                {
                    // Fallback: определяем из первого элемента
                    _logger.LogWarning(
                        "Schema not found for '{Element}', using dynamic detection",
                        rootElementName);

                    using var tempReader = XmlReader.Create(xmlPath, new XmlReaderSettings
                    {
                        Async = true,
                        IgnoreWhitespace = true,
                        IgnoreComments = true,
                        DtdProcessing = DtdProcessing.Ignore
                    });

                    var firstElement = await FindFirstDataElementAsync(tempReader, cancellationToken);
                    if (firstElement == null)
                    {
                        _logger.LogWarning("No data elements found in {XmlFile}", xmlPath);
                        return;
                    }

                    columns = GetColumnsFromElement(firstElement);
                    _logger.LogInformation(
                        "Detected {Count} columns dynamically",
                        columns.Count);
                }

                _logger.LogDebug("Columns: {Columns}", string.Join(", ", columns));

                // Создаем CSV
                Directory.CreateDirectory(Path.GetDirectoryName(csvPath) ?? ".");

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ";",
                    HasHeaderRecord = true,
                    Quote = '"',
                    Encoding = Encoding.UTF8
                };

                await using var writer = new StreamWriter(csvPath, false, Encoding.UTF8);
                await using var csv = new CsvWriter(writer, config);

                // Записываем заголовки
                foreach (var column in columns)
                {
                    csv.WriteField(column);
                }
                await csv.NextRecordAsync();

                // Обрабатываем записи потоково
                using var streamReader = new StreamReader(xmlPath, Encoding.UTF8);
                using var xmlReader = XmlReader.Create(streamReader, new XmlReaderSettings
                {
                    Async = true,
                    IgnoreWhitespace = true,
                    IgnoreComments = true,
                    DtdProcessing = DtdProcessing.Ignore
                });

                while (await xmlReader.ReadAsync())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (xmlReader.NodeType == XmlNodeType.Element &&
                        xmlReader.Depth > 0)
                    {
                        var element = (XElement)XNode.ReadFrom(xmlReader);

                        WriteRecord(csv, element, columns);
                        await csv.NextRecordAsync();
                        recordCount++;

                        if (recordCount % 10000 == 0)
                        {
                            _logger.LogDebug("Processed {Count} records...", recordCount);
                        }
                    }
                }

                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation(
                    "Converted {Records} records from {XmlFile} in {Duration:g}",
                    recordCount,
                    Path.GetFileName(xmlPath),
                    duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting {XmlFile} to CSV", xmlPath);
                throw;
            }
        }

        /// <summary>
        /// Определяет имя корневого элемента данных
        /// </summary>
        private string? GetRootDataElementName(string xmlPath)
        {
            try
            {
                using var reader = XmlReader.Create(xmlPath, new XmlReaderSettings
                {
                    IgnoreWhitespace = true,
                    IgnoreComments = true,
                    DtdProcessing = DtdProcessing.Ignore
                });

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth > 0)
                    {
                        return reader.Name;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to determine root element name");
            }

            return null;
        }

        /// <summary>
        /// Конвертирует все XML файлы из директории (рекурсивно)
        /// </summary>
        public async Task ConvertDirectoryAsync(
            string xmlDirectory,
            string csvDirectory,
            CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(xmlDirectory))
            {
                throw new DirectoryNotFoundException($"XML directory not found: {xmlDirectory}");
            }

            Directory.CreateDirectory(csvDirectory);

            // Рекурсивный поиск всех XML файлов
            var xmlFiles = Directory.GetFiles(xmlDirectory, "*.xml", SearchOption.AllDirectories)
                .ToList();

            _logger.LogInformation("Found {Count} XML files to convert (recursive search)", xmlFiles.Count);

            var results = new List<ConversionResult>();

            foreach (var xmlFile in xmlFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Сохраняем структуру подпапок в выходной директории
                var relativePath = Path.GetRelativePath(xmlDirectory, xmlFile);
                var csvFile = Path.Combine(
                    csvDirectory,
                    Path.ChangeExtension(relativePath, ".csv"));

                // Создаём подпапки если нужно
                var csvFileDirectory = Path.GetDirectoryName(csvFile);
                if (!string.IsNullOrEmpty(csvFileDirectory))
                {
                    Directory.CreateDirectory(csvFileDirectory);
                }

                try
                {
                    await ConvertXmlToCsvAsync(xmlFile, csvFile, cancellationToken);
                    results.Add(new ConversionResult
                    {
                        XmlFile = xmlFile,
                        CsvFile = csvFile,
                        Success = true
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to convert {XmlFile}", xmlFile);
                    results.Add(new ConversionResult
                    {
                        XmlFile = xmlFile,
                        Success = false,
                        Error = ex.Message
                    });
                }
            }

            // Логируем итоговую статистику
            var successCount = results.Count(r => r.Success);
            var failCount = results.Count(r => !r.Success);

            _logger.LogInformation(
                "Conversion complete: {Success} succeeded, {Failed} failed",
                successCount,
                failCount);

            if (failCount > 0)
            {
                _logger.LogWarning("Failed files: {Files}",
                    string.Join(", ", results.Where(r => !r.Success).Select(r => Path.GetFileName(r.XmlFile))));
            }
        }

        private async Task<XElement?> FindFirstDataElementAsync(
            XmlReader reader,
            CancellationToken cancellationToken)
        {
            while (await reader.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (reader.NodeType == XmlNodeType.Element && reader.Depth > 0)
                {
                    return (XElement)XNode.ReadFrom(reader);
                }
            }

            return null;
        }

        private List<string> GetColumnsFromElement(XElement element)
        {
            var columns = new List<string>();

            // Атрибуты
            foreach (var attr in element.Attributes())
            {
                columns.Add(attr.Name.LocalName);
            }

            // Дочерние элементы (если есть текстовые значения)
            foreach (var child in element.Elements())
            {
                if (!string.IsNullOrWhiteSpace(child.Value))
                {
                    columns.Add(child.Name.LocalName);
                }
            }

            return columns;
        }

        private void WriteRecord(CsvWriter csv, XElement element, List<string> columns)
        {
            foreach (var column in columns)
            {
                var attr = element.Attribute(column);
                if (attr != null)
                {
                    csv.WriteField(attr.Value);
                }
                else
                {
                    var childElement = element.Element(column);
                    csv.WriteField(childElement?.Value ?? string.Empty);
                }
            }
        }
    }
}


