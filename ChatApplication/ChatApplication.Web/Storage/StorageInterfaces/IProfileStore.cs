using ChatApplication.Web.Dtos;

namespace ChatApplication.Storage;

public interface IProfileStore
{
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="profile"></param>
    /// <returns></returns>
    /// <throws><b>ArgumentException</b> if profile is null or any of the fields are null or empty, or any field is missing<br></br><br></br>
    /// <b>ProfileAlreadyExistsException</b> if a profile with the same username already exists</throws>
    Task AddProfile(Profile profile);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="username"></param>
    /// <returns>Profile?</returns>
    /// <throws><b>ArgumentException</b> if username is null or empty</throws>
    Task<Profile> GetProfile(string username);
    
    Task DeleteProfile(string username);
}