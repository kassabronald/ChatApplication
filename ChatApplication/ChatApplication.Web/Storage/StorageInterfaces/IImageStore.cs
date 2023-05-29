using ChatApplication.Utils;
namespace ChatApplication.Storage;

public interface IImageStore
{
    /// <summary>
    ///    Adds an image to the blob storage
    /// </summary>
    /// <param name="blobName"></param>
    /// <param name="data"></param>
    /// <param name="contentType"></param>
    /// <returns></returns>
    /// <throws><b>ArgumentException</b> is thrown if blobName is null or empty<br></br></throws>
    /// <throws><b>StorageUnavailableException</b> is thrown if storage layer cannot be reached<br></br></throws>
    Task AddImage(string blobName, MemoryStream data, string contentType);
    
    /// <summary>
    ///     Gets an image from the blob storage
    /// </summary>
    /// <param name="id"></param>
    /// <returns>ImageUtil</returns>
    /// <throws><b>ArgumentException</b> if id is null or empty<br></br><br></br>
    /// </throws>
    /// <throws><b>ImageNotFoundException</b> if no image is found for the given id<br></br></throws>
    /// <throws><b>StorageUnavailableException</b> if storage layer cannot be reached<br></br></throws>
    Task<Image?> GetImage(string id);
    
    /// <summary>
    /// Delete an existing image
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Task</returns>
    /// <throws><b>ArgumentException</b> if id is null or empty<br></br></throws>
    /// <throws><b>StorageUnavailableException</b> if storage layer cannot be reached<br></br></throws>
    Task DeleteImage(string id);
}