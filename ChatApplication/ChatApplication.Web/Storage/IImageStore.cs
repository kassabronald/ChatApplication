using ChatApplication.Utils;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.Storage;

public interface IImageStore
{
    Task AddImage(string blobName, MemoryStream data, string contentType);
    Task<ImageUtil?> GetImage(string id);
    Task DeleteImage(string id);
}