using ChatApplication.Web.Dtos;

namespace ChatApplication.Services;

public interface IProfileService
{
    /// <summary>
    /// Add a profile
    /// </summary>
    /// <param name="profile"></param>
    /// <returns>Task</returns>
    /// <throws><b>StorageUnavailableException</b> is thrown if storage layer cannot be reached<br></br></throws>
    /// <throws><b>ProfileAlreadyExistsException</b> is thrown if profile already exists<br></br></throws>
    /// <throws><b>ArgumentException</b> is thrown if profile is null<br></br></throws>
    Task AddProfile(Profile profile);
    
    /// <summary>
    /// Get a profile
    /// </summary>
    /// <param name="username"></param>
    /// <returns>Task with a profile</returns>
    /// <throws><b>StorageUnavailableException</b> is thrown if storage layer cannot be reached<br></br></throws>
    /// <throws><b>ProfileNotFoundException</b> is thrown if profile does not exist<br></br></throws>
    Task<Profile> GetProfile(string username);
}