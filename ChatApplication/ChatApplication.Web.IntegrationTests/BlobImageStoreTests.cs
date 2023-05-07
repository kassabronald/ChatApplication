using System.Runtime.InteropServices;
using ChatApplication.Exceptions;
using ChatApplication.Storage;
using ChatApplication.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApplication.Web.IntegrationTests;

public class BlobImageStoreTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly IImageStore _store;
    private readonly string blobName = Guid.NewGuid().ToString();
    private readonly MemoryStream _data = new(new byte[] { 1, 2, 3 });
    private readonly string _contentType = "image/png";
    
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _store.DeleteImage(blobName);
    }
    
    public BlobImageStoreTests(WebApplicationFactory<Program> factory)
    {
        _store = factory.Services.GetRequiredService<IImageStore>();
    }
    
    [Fact]
    
    public async Task AddImage_Success()
    {
        await _store.AddImage(blobName, _data, _contentType);
        var actual = await _store.GetImage(blobName);
        var actualData = new MemoryStream(actual!.ImageData);
        Assert.Equal(_data.ToArray(), actualData.ToArray());
        Assert.Equal(_contentType, actual.ContentType);
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
    
    public async Task AddImage_InvalidArgs(string blobName, byte[] data, string contentType)
    {
        var stream = new MemoryStream(data);
        await Assert.ThrowsAsync<ArgumentException>(() => _store.AddImage(blobName, stream, contentType));
    }
    
    [Fact]
    
    public async Task GetImage_NotFound()
    {
        await Assert.ThrowsAsync<ImageNotFoundException>(
            async () => await _store.GetImage("foobar"));
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

    public async Task DeleteImage_Success()
    {
        
        await _store.AddImage(blobName, _data, _contentType);
        await _store.DeleteImage(blobName);
        await Assert.ThrowsAsync<ImageNotFoundException>(async () =>
        {
            await _store.GetImage(blobName);
        });
    }

    [Fact]

    public async Task DeleteImage_EmptyImage()
    {
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _store.DeleteImage("");
        });
    }
    
    
}
