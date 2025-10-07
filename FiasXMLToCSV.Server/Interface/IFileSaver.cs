namespace FiasXMLToCSV.Server.Interface;
public interface IFileSaver
{
    Task SaveFileAsync(string path, byte[] data,CancellationToken cancellationToken);
}
