using ATS.Application.Common.Interfaces;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ATS.Infrastructure.Storage;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient? _container;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly string _localUploadsPath;

    public AzureBlobStorageService(IConfiguration config, ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        var connectionString = config["AzureBlobStorage:ConnectionString"];
        var containerName = config["AzureBlobStorage:ContainerName"] ?? "resumes";

        _localUploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        if (!string.IsNullOrWhiteSpace(connectionString) && !connectionString.Equals("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var options = new BlobClientOptions();
                options.Retry.MaxRetries = 1;
                options.Retry.NetworkTimeout = TimeSpan.FromSeconds(3);
                _container = new BlobContainerClient(connectionString, containerName, options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Azure Blob Container client. Falling back to local disk storage.");
                _container = null;
            }
        }
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var safeFileName = $"{Guid.NewGuid()}-{Path.GetFileName(fileName)}";

        if (_container != null)
        {
            try
            {
                await _container.CreateIfNotExistsAsync(cancellationToken: ct);
                var blobClient = _container.GetBlobClient(safeFileName);
                await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Azure Blob Storage connection failed. Falling back to local disk storage: {Message}", ex.Message);
            }
        }

        // Fallback: Local Disk Storage (when Azurite or Azure Blob Storage is not running locally)
        Directory.CreateDirectory(_localUploadsPath);
        var localFilePath = Path.Combine(_localUploadsPath, safeFileName);
        using (var destinationStream = File.Create(localFilePath))
        {
            if (fileStream.CanSeek)
                fileStream.Position = 0;
            await fileStream.CopyToAsync(destinationStream, ct);
        }

        return $"/uploads/{safeFileName}";
    }

    public async Task DeleteAsync(string blobUrl, CancellationToken ct = default)
    {
        if (blobUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            var fileName = blobUrl.Replace("/uploads/", "");
            var localFilePath = Path.Combine(_localUploadsPath, fileName);
            if (File.Exists(localFilePath))
            {
                File.Delete(localFilePath);
            }
            return;
        }

        if (_container != null)
        {
            try
            {
                var blobName = new Uri(blobUrl).Segments[^1];
                await _container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete Azure Blob: {Url}", blobUrl);
            }
        }
    }
}
