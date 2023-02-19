using ChatApplication.Storage;
using ChatApplication.Web.Dtos;


namespace ChatApplication.Storage;

public class InMemoryProfileStore : IProfileStore
{
    private readonly Dictionary<string, Profile> _profiles = new();
        
    public Task AddProfile(Profile profile)
    {
        if (profile == null ||
            string.IsNullOrWhiteSpace(profile.username) ||
            string.IsNullOrWhiteSpace(profile.firstName) ||
            string.IsNullOrWhiteSpace(profile.lastName) ||
            string.IsNullOrWhiteSpace(profile.ProfilePictureId)
           )
        {
            throw new ArgumentException($"Invalid profile {profile}", nameof(profile));
        }
        
        _profiles[profile.username] = profile;
        return Task.CompletedTask;
    }

    public Task<Profile?> GetProfile(string username)
    {
        if (!_profiles.ContainsKey(username)) return Task.FromResult<Profile?>(null);
        return Task.FromResult<Profile?>(_profiles[username]);
    }

    
}