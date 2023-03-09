using System.Text;
using ChatApplication.Storage;
using ChatApplication.Utils;

namespace ChatApplication.Web.Tests.Storage;

public class InMemoryImageStoreTests
{
    private readonly InMemoryImageStore _store = new();
    
    [Fact]
    public async Task AddImage()
    {
        var data = new MemoryStream();
        await data.WriteAsync(Encoding.UTF8.GetBytes("foobar"));
        var blobName = "foobar";
        await _store.AddImage(blobName, data, "image/jpeg");
        var image = await _store.GetImage(blobName);
        Assert.Equal(data.ToArray(), image._imageData);
        Assert.Equal("image/jpeg", image._contentType);
    }
    
    [Fact]
    public async Task GetNonExistingImage()
    {
        Assert.Null(await _store.GetImage("foobar"));
    }
    
    [Theory]
    [InlineData(null, new byte[0], "image/jpeg")]
    [InlineData("", new byte[0], "image/jpeg")]
    [InlineData(" ", new byte[0], "image/jpeg")]
    [InlineData("foobar", new byte[0], "image/jpeg")]
    [InlineData("foobar", new byte[0], "image/pdf")]
    [InlineData("foobar", new byte[0], "")]
    [InlineData("foobar", new byte[0], " ")]
    [InlineData("foobar", new byte[0], null)]

    public async Task AddImage_InvalidArgs(string blobname, byte[] data, string contentType)
    {
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _store.AddImage(blobname, new MemoryStream(data), contentType);
        });
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetImage_InvalidArgs(string id)
    {
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _store.GetImage(id);
        });
        
    }
    
    [Fact]

    public async Task DeleteProfile()
    {
        
        var data = new MemoryStream();
        await data.WriteAsync(Encoding.UTF8.GetBytes("foobar"));
        await _store.AddImage("hello", data, "image/jpeg");
        await _store.DeleteImage("hello");
        Assert.Null(await _store.GetImage("hello"));
    }

    [Fact]

    public async Task DeleteEmptyProfile()
    {
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _store.DeleteImage("");
        });
    }

    
    
    
}