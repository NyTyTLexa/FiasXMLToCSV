namespace FiasXMLToCSV.Server.Interface;
public interface IArchiveExtractor
{
    Task ExtractAsync(string archivePath, string destinationPath,CancellationToken cancellationToken);
}
