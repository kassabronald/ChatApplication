using System.Net;
using ChatApplication.Exceptions;
using ChatApplication.Services;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace ChatApplication.Web.Tests.Controllers;

public class ImagesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly Mock<IImageService> _imageServiceMock = new();
    private readonly HttpClient _httpClient;
    
    public ImagesControllerTests(WebApplicationFactory<Program> factory)
    {

        // DRY: Don't repeat yourself

        _httpClient = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services => { services.AddSingleton(_imageServiceMock.Object); });
        }).CreateClient();
    }

    
    

    [Fact]

    public async Task GetImage()
    {
        var image = new byte[] {1, 2, 3, 4, 5};
        var imageId = "123";
        
        ImageUtil expectedImageUtil = new(image, "image/jpeg");
        _imageServiceMock.Setup(m => m.GetImage(imageId)).ReturnsAsync(expectedImageUtil);
        
        var response = await _httpClient.GetAsync($"/api/Images/{imageId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var contentActual = await response.Content.ReadAsByteArrayAsync();
        var contentTypeActual = response.Content.Headers.ContentType?.ToString();
        
        Assert.Equal(expectedImageUtil.ImageData, contentActual);
        Assert.Equal(expectedImageUtil.ContentType, contentTypeActual);
        
    }

    [Fact]
    public async Task GetImageNotFound()
    {
        _imageServiceMock.Setup(m => m.GetImage("123")).ThrowsAsync(new ImageNotFoundException());
        var response = await _httpClient.GetAsync($"/api/Images/123");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    
    [Fact]
    public async Task UploadImage()
    {
        var image = new byte[] { 1, 2, 3, 4, 5 };
        var imageId = "123";
        var fileName = "test.jpeg";
        var streamFile = new MemoryStream(image);
        IFormFile file = new FormFile(streamFile, 0, streamFile.Length, "id_from_form", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };
        var uploadRequest = new UploadImageRequest(file);
        _imageServiceMock.Setup(m => m.AddImage(It.IsAny<MemoryStream>(), "image/jpeg")).ReturnsAsync(imageId);
        
        using var formData = new MultipartFormDataContent();
        var requestContent = new StreamContent(uploadRequest.File.OpenReadStream());
        requestContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        formData.Add(requestContent, "File", uploadRequest.File.FileName);
        
        var response = await _httpClient.PostAsync("/api/Images", formData);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        _imageServiceMock.Verify(mock => mock.AddImage(It.IsAny<MemoryStream>(), "image/jpeg"), Times.Once);
    }
    
    [Fact]
    
    public async Task UploadImageBadRequestContentType()
    {
        var image = new byte[] { 1, 2, 3, 4, 5 };
        var fileName = "test.pdf";
        var streamFile = new MemoryStream(image);
        IFormFile file = new FormFile(streamFile, 0, streamFile.Length, "id_from_form", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/pdf"
        };
        var uploadRequest = new UploadImageRequest(file);
        _imageServiceMock.Setup(m => m.AddImage(It.IsAny<MemoryStream>(), It.IsAny<String>()));
        
        using var formData = new MultipartFormDataContent();
        formData.Add(new StreamContent(uploadRequest.File.OpenReadStream()), "File", uploadRequest.File.FileName);
        
        var response = await _httpClient.PostAsync("/api/Images", formData);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        _imageServiceMock.Verify(mock => mock.AddImage(It.IsAny<MemoryStream>(), It.IsAny<String>()), Times.Never);
    }
    
    
    [Fact]
    
    public async Task UploadImageBadRequestEmptyFile()
    {
        var image = new byte[] {};
        var fileName = "test.jpg";
        var streamFile = new MemoryStream(image);
        IFormFile file = new FormFile(streamFile, 0, streamFile.Length, "id_from_form", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpg"
        };
        var uploadRequest = new UploadImageRequest(file);
        _imageServiceMock.Setup(m => m.AddImage(It.IsAny<MemoryStream>(), It.IsAny<String>()));
        
        using var formData = new MultipartFormDataContent();
        formData.Add(new StreamContent(uploadRequest.File.OpenReadStream()), "File", uploadRequest.File.FileName);
        
        var response = await _httpClient.PostAsync("/api/Images", formData);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        _imageServiceMock.Verify(mock => mock.AddImage(It.IsAny<MemoryStream>(), It.IsAny<String>()), Times.Never);
    }
    
    
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    


    public async Task UploadImageBadRequestId(string id)
    {
        var image = new byte[] { 1, 2, 3, 4, 5 };
        var fileName = "test.png";
        var streamFile = new MemoryStream(image);
        IFormFile file = new FormFile(streamFile, 0, streamFile.Length, "id_from_form", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
        var uploadRequest = new UploadImageRequest(file); 
        _imageServiceMock.Setup(m => m.AddImage(It.IsAny<MemoryStream>(), It.IsAny<String>()));
            
        using var formData = new MultipartFormDataContent(); 
        formData.Add(new StreamContent(uploadRequest.File.OpenReadStream()), "File", uploadRequest.File.FileName);
        var response = await _httpClient.PostAsync("/api/Images", formData);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        _imageServiceMock.Verify(mock => mock.AddImage(It.IsAny<MemoryStream>(), It.IsAny<String>()), Times.Never);
    }
    
    
    [Fact]
    public async Task GetImageInvalidId()
    {
        _imageServiceMock.Setup(m => m.GetImage("123")).ThrowsAsync(new ArgumentException());
        var response = await _httpClient.GetAsync($"/api/Images/123");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
    }
    

    
}

