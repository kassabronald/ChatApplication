using System.Text;
using ChatApplication.Storage;

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
        var id=await _store.AddImage(blobName, data, "image/jpeg");
        var image = await _store.GetImage(id);
        Assert.Equal(data.ToArray(), image.FileContents);
        Assert.Equal("image/jpeg", image.ContentType);
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

    
    
    
}