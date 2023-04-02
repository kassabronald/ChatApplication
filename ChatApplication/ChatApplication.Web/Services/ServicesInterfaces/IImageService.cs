using ChatApplication.Utils;
namespace ChatApplication.Services;

public interface IImageService
{
    Task<string> AddImage(MemoryStream data, string contentType);
    Task<ImageUtil?> GetImage(string id);
}