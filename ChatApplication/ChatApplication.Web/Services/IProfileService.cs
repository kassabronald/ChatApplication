using ChatApplication.Web.Dtos;

namespace ChatApplication.Services;

public interface IProfileService
{
    Task AddProfile(Profile profile);
    Task<Profile?> GetProfile(string username);
}