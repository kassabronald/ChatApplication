using System.Drawing;
using System.Net;
using ChatApplication.Exceptions;
using ChatApplication.Services;
using ChatApplication.Storage;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;

namespace ChatApplication.Web.Tests.Services;

public class ProfileServiceTests
{

    
    private readonly Mock<IProfileStore> _profileStoreMock = new();
    private readonly Mock<IImageStore> _imageStoreMock = new();
    private readonly ProfileService _profileService;

    public ProfileServiceTests()
    {
        _profileService = new ProfileService(_profileStoreMock.Object, _imageStoreMock.Object);
    }

    [Fact]
    public async Task GetProfile()
    {
        var profile = new Profile("foobar", "Foo", "Bar", "12345");
        _profileStoreMock.Setup(m => m.GetProfile(profile.username))
            .ReturnsAsync(profile);
        var actualProfile = await _profileService.GetProfile(profile.username);
        
        Assert.Equivalent(profile, actualProfile);
    }
    
    [Fact]
    public async Task GetProfile_NotFound()
    {
        _profileStoreMock.Setup(m => m.GetProfile("foobar"))
            .ThrowsAsync(new ProfileNotFoundException());
        
        await Assert.ThrowsAsync<ProfileNotFoundException>(async () => await _profileService.GetProfile("foobar"));
    }

    [Fact]
    public async Task AddProfile()
    {
        var profile = new Profile("foobar", "Foo", "Bar", "12345");
        var image = new byte[]{0,1,2};
        _imageStoreMock.Setup(m => m.GetImage(profile.ProfilePictureId))
            .ReturnsAsync(new ImageUtil(image, "image/png"));
        _profileStoreMock.Setup(m => m.AddProfile(profile));
        await _profileService.AddProfile(profile);
        _profileStoreMock.Verify(mock => mock.AddProfile(profile), Times.Once);
    }
    
    [Fact]
    public async Task AddProfile_InvalidImage()
    {
        var profile = new Profile("foobar", "Foo", "Bar", "12345");
        var image = new byte[]{0,1,2};
        _imageStoreMock.Setup(m => m.GetImage(profile.ProfilePictureId))
            .ThrowsAsync(new ArgumentException());
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _profileService.AddProfile(profile);
        });
        _profileStoreMock.Verify(mock => mock.AddProfile(profile), Times.Never);
    }

}