using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.Storage;

public interface IImageStore
{
    Task<string> AddImage(string blobName, MemoryStream data);
    Task<byte[]?> GetImage(string id);
}