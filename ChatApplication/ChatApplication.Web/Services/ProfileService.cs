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
        //should we add the ExistingProfile check here?
        var imageContent = await _imageStore.GetImage(profile.ProfilePictureId);
        if (imageContent == null)
        {
            throw new ArgumentException($"Image with id {profile.ProfilePictureId} does not exist");
        }
        await _profileStore.AddProfile(profile);
    }

    public async Task<Profile?> GetProfile(string username)
    {
        var profile = await _profileStore.GetProfile(username);
        return profile;
    }
}