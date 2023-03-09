using ChatApplication.Utils;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.Services;

public interface IImageService
{
    Task<string> AddImage(MemoryStream data, string contentType);
    Task<ImageUtil?> GetImage(string id);
}