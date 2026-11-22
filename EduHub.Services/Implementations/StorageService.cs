using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using EduHub.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EduHub.Services.Implementations;

public class StorageService : IStorageService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<StorageService> _logger;
    private readonly BlobServiceClient? _blobServiceClient;
    private readonly string? _blobContainerName;
    private readonly bool _useAzure;

    public StorageService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<StorageService> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;

        var azureConnectionString = _configuration["AzureBlobStorage:ConnectionString"];
        _blobContainerName = _configuration["AzureBlobStorage:ContainerName"] ?? "eduhub-media";

        if (!string.IsNullOrWhiteSpace(azureConnectionString) &&
            !azureConnectionString.Contains("UseDevelopmentStorage=true") &&
            azureConnectionString.StartsWith("DefaultEndpointsProtocol="))
        {
            try
            {
                _blobServiceClient = new BlobServiceClient(azureConnectionString);
                _useAzure = true;
                _logger.LogInformation("Azure Blob Storage client initialized successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Azure Blob Storage client. Falling back to local storage.");
                _useAzure = false;
            }
        }
        else
        {
            _useAzure = false;
            _logger.LogInformation("Azure Blob Storage connection string not provided or invalid. Using local storage fallback.");
        }
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folder = "general")
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File cannot be empty.", nameof(file));

        var extension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";

        if (_useAzure && _blobServiceClient != null && !string.IsNullOrEmpty(_blobContainerName))
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_blobContainerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                var blobClient = containerClient.GetBlobClient($"{folder}/{uniqueFileName}");
                await using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading to Azure Blob Storage, falling back to local.");
            }
        }

        // Local storage fallback
        var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var targetDir = Path.Combine(webRoot, "uploads", folder);
        Directory.CreateDirectory(targetDir);

        var filePath = Path.Combine(targetDir, uniqueFileName);
        await using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        return $"/uploads/{folder}/{uniqueFileName}";
    }

    public async Task<string> UploadStreamAsync(Stream stream, string fileName, string contentType, string folder = "general")
    {
        var extension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";

        if (_useAzure && _blobServiceClient != null && !string.IsNullOrEmpty(_blobContainerName))
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_blobContainerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                var blobClient = containerClient.GetBlobClient($"{folder}/{uniqueFileName}");
                stream.Position = 0;
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading stream to Azure Blob Storage, falling back to local.");
            }
        }

        // Local storage fallback
        var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var targetDir = Path.Combine(webRoot, "uploads", folder);
        Directory.CreateDirectory(targetDir);

        var filePath = Path.Combine(targetDir, uniqueFileName);
        stream.Position = 0;
        await using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await stream.CopyToAsync(fileStream);
        }

        return $"/uploads/{folder}/{uniqueFileName}";
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return false;

        if (_useAzure && _blobServiceClient != null && fileUrl.Contains(".blob.core.windows.net"))
        {
            try
            {
                var uri = new Uri(fileUrl);
                var containerName = uri.Segments[1].TrimEnd('/');
                var blobName = string.Join("", uri.Segments.Skip(2));

                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);
                return await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from Azure Blob: {Url}", fileUrl);
                return false;
            }
        }

        // Local file delete
        try
        {
            var relativePath = fileUrl.TrimStart('/');
            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fullPath = Path.Combine(webRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting local file: {Url}", fileUrl);
        }

        return false;
    }

    public string GenerateSecureStreamingUrl(string originalVideoUrl, int expiryMinutes = 120)
    {
        if (string.IsNullOrWhiteSpace(originalVideoUrl)) return originalVideoUrl;

        // If it's an Azure Blob URL and we have Azure client
        if (_useAzure && _blobServiceClient != null && originalVideoUrl.Contains(".blob.core.windows.net"))
        {
            try
            {
                var uri = new Uri(originalVideoUrl);
                var containerName = uri.Segments[1].TrimEnd('/');
                var blobName = string.Join("", uri.Segments.Skip(2));

                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                if (blobClient.CanGenerateSasUri)
                {
                    var sasBuilder = new BlobSasBuilder
                    {
                        BlobContainerName = containerName,
                        BlobName = blobName,
                        Resource = "b",
                        ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
                    };
                    sasBuilder.SetPermissions(BlobSasPermissions.Read);

                    return blobClient.GenerateSasUri(sasBuilder).ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate SAS token for video: {Url}", originalVideoUrl);
            }
        }

        // For local or external streaming URLs (sample videos), return URL or tokenized signature
        return originalVideoUrl;
    }
}
