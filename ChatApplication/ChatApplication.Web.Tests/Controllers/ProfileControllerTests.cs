using System.Net;
using System.Text;
using ChatApplication.Exceptions;
using ChatApplication.Services;
using ChatApplication.Web.Dtos;
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
        _profileServiceMock.Setup(m => m.GetProfile(profile.Username))
            .ReturnsAsync(profile);
        var response = await _httpClient.GetAsync($"/api/Profile/{profile.Username}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Equal(profile, JsonConvert.DeserializeObject<Profile>(json));
    }
    
    [Fact]
    public async Task GetProfile_NotFound()
    {
        _profileServiceMock.Setup(m => m.GetProfile("foobar"))
            .ThrowsAsync(new ProfileNotFoundException("Profile not found"));
        var response = await _httpClient.GetAsync($"/api/Profile/foobar");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddProfile()
    {
        var profile = new Profile("foobar", "Foo", "Bar", "12345");
        var response = await _httpClient.PostAsync("/api/Profile",
            new StringContent(JsonConvert.SerializeObject(profile), Encoding.Default, "application/json"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("http://localhost/api/Profile/foobar", response.Headers.GetValues("Location").First());
        _profileServiceMock.Verify(mock => mock.AddProfile(profile), Times.Once);
    }
    
    [Fact]
    public async Task AddProfile_Conflict()
    {
        var profile = new Profile("foobar", "Foo", "Bar", "12345");
        _profileServiceMock.Setup(m => m.AddProfile(profile))
            .ThrowsAsync(new ProfileAlreadyExistsException("Profile already exists"));

        var response = await _httpClient.PostAsync("/api/Profile",
            new StringContent(JsonConvert.SerializeObject(profile), Encoding.Default, "application/json"));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
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
    public async Task AddProfile_InvalidArgs(string username, string firstName, string lastName, string profilePictureId )
    {
        var profile = new Profile(username, firstName, lastName, profilePictureId);
        var response = await _httpClient.PostAsync("/api/Profile",
            new StringContent(JsonConvert.SerializeObject(profile), Encoding.Default, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _profileServiceMock.Verify(mock => mock.AddProfile(profile), Times.Never);
    }
    
    
    [Fact]
    public async Task AddProfile_InvalidImage()
    {
        var profile = new Profile("foobar", "Foo", "Bar", "12345");
        _profileServiceMock.Setup(m=> m.AddProfile(profile))
            .ThrowsAsync(new ImageNotFoundException("Image not found"));
        var response = await _httpClient.PostAsync("/api/Profile",
                new StringContent(JsonConvert.SerializeObject(profile), Encoding.Default, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

}