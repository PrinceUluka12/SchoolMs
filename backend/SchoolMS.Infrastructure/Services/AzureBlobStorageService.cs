using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SchoolMS.Core.Interfaces;

namespace SchoolMS.Infrastructure.Services;

public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly string _baseUrl;

    public AzureBlobStorageService(IConfiguration config)
    {
        var connectionString = config["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException("AzureStorage:ConnectionString not configured.");

        _containerName = config["AzureStorage:ContainerName"] ?? "schoolms-files";
        _baseUrl = config["AzureStorage:BaseUrl"]
            ?? throw new InvalidOperationException("AzureStorage:BaseUrl not configured.");

        _blobServiceClient = new BlobServiceClient(connectionString);
        EnsureContainerExistsAsync().GetAwaiter().GetResult();
    }

    public async Task<string> SaveFileAsync(IFormFile file, string folder)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        // Sanitise: use GUID filename to prevent collisions and path traversal
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blobName = $"{folder}/{Guid.NewGuid()}{extension}";
        var blobClient = containerClient.GetBlobClient(blobName);

        var contentType = GetContentType(extension);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });

        return blobName; // relative path stored in DB — same as local service
    }

    public async Task DeleteFileAsync(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return;

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(relativePath);
        await blobClient.DeleteIfExistsAsync();
    }

    public string GetFileUrl(string relativePath)
    {
        return $"{_baseUrl.TrimEnd('/')}/{_containerName}/{relativePath}";
    }

    private async Task EnsureContainerExistsAsync()
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
    }

    private static string GetContentType(string extension) => extension switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png"  => "image/png",
        ".webp" => "image/webp",
        ".gif"  => "image/gif",
        ".pdf"  => "application/pdf",
        ".doc"  => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".csv"  => "text/csv",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        _       => "application/octet-stream"
    };
}