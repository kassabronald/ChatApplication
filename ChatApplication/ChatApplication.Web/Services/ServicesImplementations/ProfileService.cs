using ChatApplication.Storage;
using ChatApplication.Web.Dtos;

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
        await _profileStore.AddProfile(profile);
    }

    public async Task<Profile> GetProfile(string username)
    {
        var profile = await _profileStore.GetProfile(username);
        return profile;
    }
}