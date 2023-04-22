using ChatApplication.Services;
using ChatApplication.Storage;
using ChatApplication.Utils;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChatApplication.Web.Tests.Services;

public class ImageServiceTests
{
    private readonly Mock<IImageStore> _imageStoreMock = new();
    private readonly ImageService _imageService;
    private readonly Mock<ILogger<ImageService>> _logger = new();
    
    public ImageServiceTests()
    {
        _imageService = new ImageService(_imageStoreMock.Object, _logger.Object);
    }
    
    [Fact]
    public async Task GetImage()
    {
        var image = new byte[]{0,1,2};
        _imageStoreMock.Setup(m => m.GetImage("12345"))
            .ReturnsAsync(new Image(image, "image/png"));
        var actualImage = await _imageService.GetImage("12345");
        Assert.Equal(image, actualImage?.ImageData);
    }
    
    [Fact]
    public async Task GetImage_NotFound()
    {
        _imageStoreMock.Setup(m => m.GetImage("12345"))
            .ReturnsAsync((Image?)null);
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
}