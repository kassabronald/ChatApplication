using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.Storage;

public interface IImageStore
{
    Task<string?> AddImage(string blobName, MemoryStream data, string contentType);
    Task<FileContentResult?> GetImage(string id);
}