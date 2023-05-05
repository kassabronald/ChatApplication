using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ChatApplication.Exceptions;
using ChatApplication.Exceptions.StorageExceptions;
using ChatApplication.Utils;

namespace ChatApplication.Storage;

public class BlobImageStore : IImageStore
{
    private readonly BlobContainerClient _blobContainerClient;

    public BlobImageStore(BlobContainerClient blobContainerClientClient)
    {
        _blobContainerClient = blobContainerClientClient;
    }

    public async Task AddImage(string blobName, MemoryStream data, string contentType)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data is empty", nameof(data));

        BlobClient blobClient = _blobContainerClient.GetBlobClient(blobName);
        BlobHttpHeaders headers = new BlobHttpHeaders
        {
            ContentType = contentType
        };
        data.Position = 0;

        try
        {
            await blobClient.UploadAsync(data, headers);
        }
        catch (RequestFailedException)
        {
            throw new StorageUnavailableException("Blob storage is unavailable");
        }
        
    }

    public async Task<Image?> GetImage(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Invalid id", nameof(id));
        }

        var blobClient = _blobContainerClient.GetBlobClient(id);
        if (!await blobClient.ExistsAsync())
            throw new ImageNotFoundException($"No image found for {id}");
        
        BlobProperties properties = await blobClient.GetPropertiesAsync();

        try
        {
            var response = await blobClient.DownloadAsync();

            await using var memoryStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            return new Image(bytes, properties.ContentType);
        }
        catch (RequestFailedException)
        {
            throw new StorageUnavailableException("Blob storage is unavailable");
        }
        
    }

    public async Task DeleteImage(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Invalid id", nameof(id));
        }
    
        var blobClient = _blobContainerClient.GetBlobClient(id);
        if (!await blobClient.ExistsAsync())
        {
            return;
        }

        try
        {
            await blobClient.DeleteAsync();
        }
        catch (RequestFailedException)
        {
            throw new StorageUnavailableException("Blob storage is unavailable");
        }
    }
}

