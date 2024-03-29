﻿using Azure;
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
        catch (RequestFailedException e)
        {
            throw new StorageUnavailableException($"Could not upload image {blobName}", e);
        }
        
    }

    public async Task<Image?> GetImage(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Invalid id", nameof(id));
        }

        try
        {
            
            var blobClient = _blobContainerClient.GetBlobClient(id);
            
            if (!await blobClient.ExistsAsync())
                throw new ImageNotFoundException($"No image found for {id}");
            
            BlobProperties properties = await blobClient.GetPropertiesAsync();
            var response = await blobClient.DownloadAsync();
            await using var memoryStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            return new Image(bytes, properties.ContentType);
        }
        catch (RequestFailedException e)
        {
            throw new StorageUnavailableException($"Could not download image {id}", e);
        }
        
    }

    public async Task DeleteImage(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Invalid id", nameof(id));
        }
    
        try
        {
            var blobClient = _blobContainerClient.GetBlobClient(id);
            if (!await blobClient.ExistsAsync())
            {
                return;
            }
            await blobClient.DeleteAsync();
        }
        catch (RequestFailedException e)
        {
            throw new StorageUnavailableException($"Could not delete image {id}", e);
        }
    }
}

