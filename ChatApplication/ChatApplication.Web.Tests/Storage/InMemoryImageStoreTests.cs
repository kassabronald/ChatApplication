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
        var id=await _store.AddImage(blobName, data);
        Assert.Equal(data.ToArray(), await _store.GetImage(id));
    }
    
    [Fact]
    public async Task GetNonExistingImage()
    {
        Assert.Null(await _store.GetImage("foobar"));
    }
    
    [Theory]
    [InlineData(null, new byte[0])]
    [InlineData("", new byte[0])]
    [InlineData(" ", new byte[0])]
    [InlineData("foobar", new byte[0])]
    public async Task AddImage_InvalidArgs(string blobname, byte[] data)
    {
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _store.AddImage(blobname, new MemoryStream(data));
        });
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetImage_InvalidArgs(string id)
    {
        Assert.Null( await _store.GetImage(id));
        
    }
    
    
}