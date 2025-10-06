using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using UpdateGARBDFIAS.Interface;
using UpdateGARBDFIAS.Models;

namespace UpdateGARBDFIAS.Infrastructure;
public class HttpFileDownloader : IFileDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpFileDownloader> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly FiasDownloadOptions _options;

        public HttpFileDownloader(
            HttpClient httpClient,
            ILogger<HttpFileDownloader> logger,
            IOptions<FiasDownloadOptions> options)
        {
            _httpClient = httpClient;
            _logger = logger;
            _options = options.Value;

            // Configure Polly retry policy with exponential backoff
            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    _options.MaxRetries,
                    retryAttempt => TimeSpan.FromMilliseconds(_options.RetryDelayMs * retryAttempt),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            exception,
                            "Download attempt {RetryCount} failed. Retrying in {Delay}ms...",
                            retryCount,
                            timeSpan.TotalMilliseconds);
                    });
        }

        public async Task<byte[]> DownloadFileAsync(string url, CancellationToken cancellationToken = default)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                _logger.LogInformation("Downloading from {Url}", url);

                using var response = await _httpClient.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Download failed with status {StatusCode}: {ReasonPhrase}",
                        response.StatusCode,
                        response.ReasonPhrase);
                    response.EnsureSuccessStatusCode();
                }

                var data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                _logger.LogInformation("Downloaded {Size:N0} bytes successfully", data.Length);

                return data;
            });
        }
    }

