using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;

namespace ChatApplication.Storage;

public class BlobImageStore: IImageStore
{
    
    private readonly BlobContainerClient _blobContainerClient;
    
    public BlobImageStore(BlobContainerClient blobContainerClientClient)
    {
        _blobContainerClient = blobContainerClientClient;
    }
    public Task<string?> AddImage(string blobName, MemoryStream data, string contentType)
    {
        throw new NotImplementedException();
    }

    public Task<FileContentResult?> GetImage(string id)
    {
        throw new NotImplementedException();
    }
}