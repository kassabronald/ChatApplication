using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.Storage;

public interface IImageStore
{
    Task AddImage(string blobName, byte[] image);
    Task<byte[]?> GetImage(string id);
}