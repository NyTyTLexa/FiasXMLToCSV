namespace FiasXMLToCSV.Server.Interface
{
    public interface IFileDownloader
    {
        Task<byte[]> DownloadFileAsync(string url, CancellationToken cancellationToken = default);
    }
}
