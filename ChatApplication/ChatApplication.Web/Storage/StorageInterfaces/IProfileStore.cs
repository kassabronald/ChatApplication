using ChatApplication.Web.Dtos;
namespace ChatApplication.Storage;

public interface IProfileStore
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="profile"></param>
    /// <returns>Task</returns>
    /// <throws><b>ArgumentException</b> if profile is null or any of the fields are null or empty, or any field is missing<br></br>
    /// <b>ProfileAlreadyExistsException</b> if a profile with the same username already exists<br></br>
    /// <b>StorageUnavailableException</b> if storage layer cannot be reached<br></br></throws>
    /// 
    Task AddProfile(Profile profile);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="username"></param>
    /// <returns>Task with Profile</returns>
    /// <throws><b>ProfileNotFoundException</b> if profile is not found<br></br></throws>
    /// <throws><b>StorageUnavailableException</b> if storage layer cannot be reached</throws>
    Task<Profile> GetProfile(string username);
    
    /// <summary>
    /// Delete a profile
    /// </summary>
    /// <param name="username"></param>
    /// <returns>Task</returns>
    /// <throws><b>StorageUnavailableException</b> if storage layer cannot be reached<br></br></throws>
    Task DeleteProfile(string username);
}