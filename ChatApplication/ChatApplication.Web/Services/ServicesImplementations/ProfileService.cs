using ChatApplication.Storage;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;
using Microsoft.Azure.Cosmos;

namespace ChatApplication.Services;

public class ProfileService : IProfileService
{
    private readonly IProfileStore _profileStore;
    private readonly IImageStore _imageStore;
    
    public ProfileService(IProfileStore profileStore, IImageStore imageStore)
    {
        _profileStore = profileStore;
        _imageStore = imageStore;
    }
    
    
    public async Task AddProfile(Profile profile)
    {
        var imageContent = await _imageStore.GetImage(profile.ProfilePictureId);
        await _profileStore.AddProfile(profile);

    }

    public async Task<Profile> GetProfile(string username)
    {
        var profile = await _profileStore.GetProfile(username);
        return profile;
    }
}