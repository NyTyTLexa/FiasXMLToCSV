namespace FiasXMLToCSV.Server.Interface
{
    public interface IXmlToCsvConverter
    {
        Task ConvertXmlToCsvAsync(string xmlPath, string csvPath, CancellationToken cancellationToken = default);
        Task ConvertDirectoryAsync(string xmlDirectory, string csvDirectory, CancellationToken cancellationToken = default);
    }
}
