using Microsoft.AspNetCore.Mvc;
using FormFile = Microsoft.AspNetCore.Http.FormFile;

namespace ChatApplication.Storage;

public class InMemoryImageStore : IImageStore
{
    private readonly Dictionary<string, FileContentResult> _images = new();

    public async Task<string?> AddImage(string blobName, MemoryStream data, string contentType)
    {
        if (data==null || data.Length==0 || string.IsNullOrWhiteSpace(blobName) || contentType != "image/jpeg" && contentType != "image/png" && contentType != "image/jpg")
        {
            throw new ArgumentException($"Missing arguments");
        }

        var id="";
        while (_images.ContainsKey(id = Guid.NewGuid().ToString()))
        {
        }

        _images.Add(id, new FileContentResult(data.ToArray(), contentType));
        return id;
    }

    public async Task<FileContentResult?> GetImage(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }
        return _images.TryGetValue(id, out var image) ? image : null;
    }
}
