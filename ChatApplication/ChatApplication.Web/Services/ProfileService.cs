using ChatApplication.Storage;
using ChatApplication.Web.Dtos;

namespace ChatApplication.Services;

public class ProfileService : IProfileService
{
    private readonly IProfileStore _profileStore;
    
    ProfileService(IProfileStore profileStore)
    {
        _profileStore = profileStore;
    }
    
    
    public async Task AddProfile(Profile profile)
    {
        
    }

    public Task<Profile?> GetProfile(string username)
    {
        throw new NotImplementedException();
    }
}