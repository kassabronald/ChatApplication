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
using Moq;
using Newtonsoft.Json;

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
        _imageStoreMock.Setup(m => m.GetImage(imageId)).ReturnsAsync(image);
        var response = await _httpClient.GetAsync($"/Images/{imageId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        FileContent fileContentResult = new(image, "image/jpeg");
        var json = await response.Content.ReadAsStringAsync();
        var actualFileContent = JsonConvert.DeserializeObject<FileContent>(json);
        Assert.Equivalent(fileContentResult,actualFileContent);
    }

    [Fact]
    public async Task GetImageNotFound()
    {
        _imageStoreMock.Setup(m=> m.GetImage("123")).ReturnsAsync((byte[]?)null);
        var response = await _httpClient.GetAsync($"/Images/123");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]

    public async Task UploadImage()
    {
        var image = new byte[] {1, 2, 3, 4, 5};
        var imageId = "123";
        
        var fileName = "test.jpeg";
        var streamFile = new MemoryStream();
        var writer = new StreamWriter(streamFile);
        writer.Write(image);
        await writer.FlushAsync();
        streamFile.Position = 0;
        IFormFile file = new FormFile(streamFile, 0, streamFile.Length, "id_from_form", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };
        var uploadRequest = new UploadImageRequest(file);
        var streamRequest = new MemoryStream();
        await uploadRequest.File.CopyToAsync(streamRequest);
        
        _imageStoreMock.Setup(m => m.AddImage(uploadRequest.File.FileName, streamRequest)).ReturnsAsync(imageId);
        var response = await _httpClient.PostAsync("/Images",
            new StringContent(JsonConvert.SerializeObject(uploadRequest), Encoding.Default, "application/json"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("http://localhost/Images/123", response.Headers.GetValues("Location").First());
        _imageStoreMock.Verify(mock => mock.AddImage(uploadRequest.File.FileName, streamRequest), Times.Once);
    }
    
}

