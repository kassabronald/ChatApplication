using ChatApplication.Utils;
namespace ChatApplication.Services;

public interface IImageService
{
    /// <summary>
    /// Add an image to the blob storage
    /// </summary>
    /// <param name="data"></param>
    /// <param name="contentType"></param>
    /// <returns>Task with string</returns>
    /// <throws><b>ArgumentException</b> if data is null or empty<br></br></throws>
    /// <throws><b>StorageUnavailableException</b> if storage layer cannot be reached<br></br></throws>
    Task<string> AddImage(MemoryStream data, string contentType);
    
    /// <summary>
    /// Get an existing image
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Task with Image?</returns>
    /// <throws><b>ArgumentException</b> if id is null or empty<br></br></throws>
    /// <throws><b>ImageNotFoundException</b> if no image is found for the given id<br></br></throws>
    /// <throws><b>StorageUnavailableException</b> if storage layer cannot be reached<br></br></throws>
    Task<Image?> GetImage(string id);
}