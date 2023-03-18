using ChatApplication.Utils;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.Storage;

public class InMemoryImageStore : IImageStore
{
    private readonly Dictionary<string, ImageUtil> _images = new();

    public async Task AddImage(string blobName, MemoryStream data, string contentType)
    {
        if (data==null || data.Length==0 || string.IsNullOrWhiteSpace(blobName) || contentType != "image/jpeg" && contentType != "image/png" && contentType != "image/jpg")
        {
            throw new ArgumentException($"Missing arguments");
        }


        _images.Add(blobName, new ImageUtil(data.ToArray(), contentType));
    }

    public async Task<ImageUtil?> GetImage(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Invalid id", nameof(id));  
        }
        return _images.TryGetValue(id, out var image) ? image : null;
    }
    
    public async Task DeleteImage(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Invalid id", nameof(id));  
        }
        _images.Remove(id);
    }
}
