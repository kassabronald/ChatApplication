using System.Net;
using ChatApplication.Storage;
using ChatApplication.Web.Dtos;
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
    
}

