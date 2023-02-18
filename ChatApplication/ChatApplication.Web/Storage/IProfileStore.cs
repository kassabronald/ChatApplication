using ChatApplication.Web.Dtos;

namespace ChatApplication.Storage;

public interface IProfileStore
{
    Task AddProfile(Profile profile);
    Task<Profile?> GetProfile(string username);
}