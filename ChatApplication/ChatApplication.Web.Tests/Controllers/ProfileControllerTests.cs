using System.Net;
using System.Text;
using ChatApplication.Services;
using ChatApplication.Storage;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
namespace ChatApplication.Web.Tests.Controllers;




public class ProfileControllerTests: IClassFixture<WebApplicationFactory<Program>>
{
    private readonly Mock<IProfileService> _profileServiceMock = new();
    private readonly HttpClient _httpClient;
    
    public ProfileControllerTests(WebApplicationFactory<Program> factory)
    {
        _httpClient = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services => { services.AddSingleton(_profileServiceMock.Object); });
        }).CreateClient();
    }

    [Fact]
    public async Task GetProfile()
    {
        var profile = new Profile("foobar", "Foo", "Bar", "12345");
        _profileServiceMock.Setup(m => m.GetProfile(profile.username))
            .ReturnsAsync(profile);
        
        var response = await _httpClient.GetAsync($"/Profile/{profile.username}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Equal(profile, JsonConvert.DeserializeObject<Profile>(json));
    }
    
    [Fact]
    public async Task GetProfile_NotFound()
    {
        _profileServiceMock.Setup(m => m.GetProfile("foobar"))
            .ReturnsAsync((Profile?)null);

        var response = await _httpClient.GetAsync($"/Profile/foobar");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddProfile()
    {
        var profile = new Profile("foobar", "Foo", "Bar", "12345");
        var response = await _httpClient.PostAsync("/Profile",
            new StringContent(JsonConvert.SerializeObject(profile), Encoding.Default, "application/json"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("http://localhost/Profile/foobar", response.Headers.GetValues("Location").First());
        _profileServiceMock.Verify(mock => mock.AddProfile(profile), Times.Once);
    }
    
    [Fact]
    public async Task AddProfile_Conflict()
    {
        var profile = new Profile("foobar", "Foo", "Bar", "12345");
        _profileServiceMock.Setup(m => m.GetProfile(profile.username))
            .ReturnsAsync(profile);

        var response = await _httpClient.PostAsync("/Profile",
            new StringContent(JsonConvert.SerializeObject(profile), Encoding.Default, "application/json"));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        
        _profileServiceMock.Verify(m => m.AddProfile(profile), Times.Never);
    }
    
    [Theory]
    [InlineData(null, "Foo", "Bar", "12345")]
    [InlineData("", "Foo", "Bar", "12345")]
    [InlineData(" ", "Foo", "Bar", "12345")]
    [InlineData("foobar", null, "Bar", "12345")]
    [InlineData("foobar", "", "Bar", "12345")]
    [InlineData("foobar", "   ", "Bar", "12345")]
    [InlineData("foobar", "Foo", "", "12345")]
    [InlineData("foobar", "Foo", null, "12345")]
    [InlineData("foobar", "Foo", " ", "12345")]
    [InlineData("foobar", "Foo", "Bar", "")]
    [InlineData("foobar", "Foo", "Bar", " ")]
    [InlineData("foobar", "Foo", "Bar", null)]
    public async Task AddProfile_InvalidArgs(string username, string firstName, string lastName, string ProfilePictureId )
    {
        var profile = new Profile(username, firstName, lastName, ProfilePictureId);
        var response = await _httpClient.PostAsync("/Profile",
            new StringContent(JsonConvert.SerializeObject(profile), Encoding.Default, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _profileServiceMock.Verify(mock => mock.AddProfile(profile), Times.Never);
    }
    
    
    [Fact]
    public async Task AddProfile_InvalidImage()
    {
        var profile = new Profile("foobar", "Foo", "Bar", "12345");
        _profileServiceMock.Setup(m=> m.AddProfile(profile))
            .ThrowsAsync(new ArgumentException());
        var response = await _httpClient.PostAsync("/Profile",
                new StringContent(JsonConvert.SerializeObject(profile), Encoding.Default, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

}