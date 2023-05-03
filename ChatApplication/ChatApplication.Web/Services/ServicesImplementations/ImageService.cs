using ChatApplication.Storage;
using ChatApplication.Utils;

namespace ChatApplication.Services;

public class ImageService : IImageService
{
    private readonly IImageStore _imageStore;
    public ImageService(IImageStore imageStore, ILogger<ImageService> logger)
    {
        _imageStore = imageStore;
    }
    public async Task<string> AddImage(MemoryStream data, string contentType)
    {
        var id = Guid.NewGuid().ToString();
        await _imageStore.AddImage(id, data, contentType);
        return id;
    }

    public async Task<Image?> GetImage(string id)
    {
        return await _imageStore.GetImage(id);
    }
}