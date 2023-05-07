using ChatApplication.Exceptions;
using ChatApplication.Services;
using ChatApplication.Storage;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;
using Moq;

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
        
        _profileStoreMock.Setup(m => m.GetProfile(profile.Username))
            .ReturnsAsync(profile);
        
        var actualProfile = await _profileService.GetProfile(profile.Username);
        
        Assert.Equivalent(profile, actualProfile);
    }
    
    [Fact]
    public async Task GetProfile_NotFound()
    {
        _profileStoreMock.Setup(m => m.GetProfile("foobar"))
            .ThrowsAsync(new ProfileNotFoundException("Profile not found"));
        
        await Assert.ThrowsAsync<ProfileNotFoundException>(async () => await _profileService.GetProfile("foobar"));
    }

    [Fact]
    public async Task AddProfile()
    {
        var profile = new Profile("foobar", "Foo", "Bar", "12345");
        var image = new byte[]{0,1,2};
        
        _imageStoreMock.Setup(m => m.GetImage(profile.ProfilePictureId))
            .ReturnsAsync(new Image(image, "image/png"));
        _profileStoreMock.Setup(m => m.AddProfile(profile));
        
        await _profileService.AddProfile(profile);
        
        _profileStoreMock.Verify(mock => mock.AddProfile(profile), Times.Once);
    }
    
    /*[Fact]
    public async Task AddProfile_InvalidImage()
    {
        var profile = new Profile("foobar", "Foo", "Bar", "12345");
        _imageStoreMock.Setup(m => m.GetImage(profile.ProfilePictureId))
            .ThrowsAsync(new ArgumentException());
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _profileService.AddProfile(profile);
        });
        _profileStoreMock.Verify(mock => mock.AddProfile(profile), Times.Never);
    }*/
    
    //code commented for the sake of the functional tests

}