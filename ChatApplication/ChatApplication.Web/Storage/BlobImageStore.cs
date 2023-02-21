using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ChatApplication.Storage;

public class BlobImageStore: IImageStore
{
    
    private readonly BlobContainerClient _blobContainerClient;
    
    public BlobImageStore(BlobContainerClient blobContainerClientClient)
    {
        _blobContainerClient = blobContainerClientClient;
    }
    public async Task<string?> AddImage(string blobName, MemoryStream data, string contentType)
    {
        if(data==null || data.Length==0)
            throw new ArgumentException("Data is empty", nameof(data));
        if (contentType != "image/png" && contentType != "image/jpeg" && contentType != "image/jpg")
            throw new ArgumentException("Invalid content type", nameof(contentType));
        var id = Guid.NewGuid().ToString();
        BlobClient blobClient = _blobContainerClient.GetBlobClient(id);
        BlobHttpHeaders headers = new BlobHttpHeaders
        {
            ContentType = contentType
        };
        data.Position = 0;
        await blobClient.UploadAsync(data, headers);
        return id;
    }

    public async Task<FileContentResult?> GetImage(string id)
    {
        if (String.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Invalid id", nameof(id));
        }
        var blobClient =  _blobContainerClient.GetBlobClient(id);
        if(!await blobClient.ExistsAsync())
            return null;
        BlobProperties properties = await blobClient.GetPropertiesAsync();
        var response = await blobClient.DownloadAsync();
        
        await using var memoryStream = new MemoryStream();
        await response.Value.Content.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        return new FileContentResult(bytes, properties.ContentType);
    }
}