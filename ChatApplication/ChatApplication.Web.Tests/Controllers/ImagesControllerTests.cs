using System.Net;
using System.Text;
using ChatApplication.Storage;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Moq;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;
using static Microsoft.Net.Http.Headers.MediaTypeHeaderValue;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace ChatApplication.Web.Tests.Controllers;

public class ImagesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly Mock<IImageStore> _imageStoreMock = new();
    private readonly HttpClient _httpClient;
    
    public ImagesControllerTests(WebApplicationFactory<Program> factory)
    {

        // DRY: Don't repeat yourself

        _httpClient = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services => { services.AddSingleton(_imageStoreMock.Object); });
        }).CreateClient();
    }

    
    

    [Fact]

    public async Task GetImage()
    {
        var image = new byte[] {1, 2, 3, 4, 5};
        var imageId = "123";
        
        FileContentResult fileContentResultExpected = new(image, "image/jpeg");
        _imageStoreMock.Setup(m => m.GetImage(imageId)).ReturnsAsync(fileContentResultExpected);
        
        var response = await _httpClient.GetAsync($"/Images/{imageId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var contentActual = await response.Content.ReadAsByteArrayAsync();
        var contentTypeActual = response.Content.Headers.ContentType?.ToString();
        
        Assert.Equal(fileContentResultExpected.FileContents, contentActual);
        Assert.Equal(fileContentResultExpected.ContentType, contentTypeActual);
        
    }

    [Fact]
    public async Task GetImageNotFound()
    {
        var response = await _httpClient.GetAsync($"/Images/123");
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
        _imageStoreMock.Setup(m => m.AddImage(It.IsAny<String>(), It.IsAny<MemoryStream>(), It.IsAny<String>())).ReturnsAsync(imageId);
        
        using var formData = new MultipartFormDataContent();
        var requestContent = new StreamContent(uploadRequest.File.OpenReadStream());
        requestContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        formData.Add(requestContent, "File", uploadRequest.File.FileName);
        
        var response = await _httpClient.PostAsync("/Images", formData);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("http://localhost/Images/123", response.Headers.Location?.ToString());
        
        var uploadImageResponseActual = new UploadImageResponse(imageId);
        var json = await response.Content.ReadAsStringAsync();
        var uploadImageResponseExpected = JsonConvert.DeserializeObject<UploadImageResponse>(json);
        
        Assert.Equal(uploadImageResponseExpected, uploadImageResponseActual);
        _imageStoreMock.Verify(mock => mock.AddImage(It.IsAny<String>(), It.IsAny<MemoryStream>(), It.IsAny<String>()), Times.Once);
    }
    
    [Fact]
    
    public async Task UploadImageBadRequest()
    {
        var image = new byte[] { 1, 2, 3, 4, 5 };
        var imageId = "123";
        var fileName = "test.pdf";
        var streamFile = new MemoryStream(image);
        IFormFile file = new FormFile(streamFile, 0, streamFile.Length, "id_from_form", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/pdf"
        };
        var uploadRequest = new UploadImageRequest(file);
        _imageStoreMock.Setup(m => m.AddImage(It.IsAny<String>(), It.IsAny<MemoryStream>(), It.IsAny<String>())).ReturnsAsync(imageId);
        
        using var formData = new MultipartFormDataContent();
        formData.Add(new StreamContent(uploadRequest.File.OpenReadStream()), "File", uploadRequest.File.FileName);
        
        var response = await _httpClient.PostAsync("/Images", formData);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        _imageStoreMock.Verify(mock => mock.AddImage(It.IsAny<String>(), It.IsAny<MemoryStream>(), It.IsAny<String>()), Times.Never);
    }

    
}

