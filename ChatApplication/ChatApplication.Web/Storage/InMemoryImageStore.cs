namespace ChatApplication.Storage;

public class InMemoryImageStore : IImageStore
{
    private readonly Dictionary<string, byte[]> _images = new();

    public async Task<string?> AddImage(string blobName, MemoryStream data)
    {
        if (data==null || data.Length==0 || string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException($"Missing arguments");
        }

        var id="";
        while (_images.ContainsKey(id = Guid.NewGuid().ToString()))
        {
        }

        _images.Add(id, data.ToArray());
        return id;
    }

    public async Task<byte[]?> GetImage(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }
        if (_images.TryGetValue(id, out var image))
        {
            return image;
        }

        return null;
    }
}
