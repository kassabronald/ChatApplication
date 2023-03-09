using ChatApplication.Services;
using ChatApplication.Storage;
using ChatApplication.Utils;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ChatApplication.Web.Tests.Services;

public class ImageServiceTests
{
    private readonly Mock<IImageStore> _imageStoreMock = new();
    private readonly ImageService _imageService;
    
    public ImageServiceTests()
    {
        _imageService = new ImageService(_imageStoreMock.Object);
    }
    
    [Fact]
    public async Task GetImage()
    {
        var image = new byte[]{0,1,2};
        _imageStoreMock.Setup(m => m.GetImage("12345"))
            .ReturnsAsync(new ImageUtil(image, "image/png"));
        var actualImage = await _imageService.GetImage("12345");
        Assert.Equal(image, actualImage?._imageData);
    }
    
    [Fact]
    public async Task GetImage_NotFound()
    {
        _imageStoreMock.Setup(m => m.GetImage("12345"))
            .ReturnsAsync((ImageUtil?)null);
        var actualImage = await _imageService.GetImage("12345");
        Assert.Null(actualImage);
    }
    
    [Fact]
    public async Task AddImage()
    {
        var image = new byte[]{0,1,2};
        var stream = new MemoryStream(image);
        _imageStoreMock.Setup(m => m.AddImage(It.IsAny<string>(), stream, "image/png"));
        await _imageService.AddImage(stream, "image/png");
        _imageStoreMock.Verify(mock => mock.AddImage(It.IsAny<string>(), stream, "image/png"), Times.Once);
    }
    
    //[Fact]
    
    // public async Task AddImage_InvalidImage()
    // {
    //     var image = new byte[]{0,1,2};
    //     var stream = new MemoryStream(image);
    //     _imageStoreMock.Setup(m => m.AddImage(It.IsAny<string>(), stream, "image/png"))
    //         .ThrowsAsync(new InvalidImageException());
    //     await Assert.ThrowsAsync<InvalidImageException>(() => _imageService.AddImage(stream, "image/png"));
    // }
    
}