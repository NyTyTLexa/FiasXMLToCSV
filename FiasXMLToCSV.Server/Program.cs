using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using FiasXMLToCSV.Server.Infrastructure;
using FiasXMLToCSV.Server.Interface;
using FiasXMLToCSV.Server.Models;
using FiasXMLToCSV.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FiasDownloadOptions>(
    builder.Configuration.GetSection("FiasDownload"));

builder.Services.AddHttpClient<IFileDownloader, HttpFileDownloader>((serviceProvider, client) =>
{
    client.Timeout = TimeSpan.FromMinutes(10);
    client.DefaultRequestHeaders.Add("User-Agent", "FiasXMLToCSV.Server/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new SocketsHttpHandler
    {
        AllowAutoRedirect = true,
        AutomaticDecompression = DecompressionMethods.All,
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        SslOptions = new SslClientAuthenticationOptions
        {
            EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                                 System.Security.Authentication.SslProtocols.Tls13,
            
            RemoteCertificateValidationCallback = builder.Environment.IsDevelopment() &&
                builder.Configuration.GetValue<bool>("FiasDownload:AllowInvalidCertificates")
                ? (sender, cert, chain, errors) =>
                {
                    
                    if (errors != SslPolicyErrors.None)
                    {
                        Console.WriteLine($"⚠️  Certificate validation warning: {errors}");
                    }
                    return true;
                }
            : null 
        }
    };
    return handler;
});

builder.Services.AddTransient<IFileSaver, LocalFileSaver>();
builder.Services.AddTransient<IArchiveExtractor, ZipArchiveExtractor>();
builder.Services.AddTransient<DownloadService>();

builder.Services.AddSingleton<FiasSchemaParser>();
builder.Services.AddSingleton<IXmlToCsvConverter, FiasXmlToCsvConverter>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "FIAS Downloader API",
        Version = "v1",
        Description = "API for downloading and converting FIAS (GAR) data"
    });
});

// Build the application (AFTER all service registrations)
var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FIAS Downloader API v1");
    });
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();