﻿using ChatApplication.Utils;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc;

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
    /// <throws><b>ArgumentException</b> is thrown if blobName is null or empty</throws>
    Task AddImage(string blobName, MemoryStream data, string contentType);
    
    /// <summary>
    ///     Gets an image from the blob storage
    /// </summary>
    /// <param name="id"></param>
    /// <returns>ImageUtil</returns>
    /// <throws><b>ArgumentException</b> if id is null or empty<br></br><br></br>
    /// </throws>
    /// <throws><b>ImageNotFoundException</b> if no image is found for the given id</throws>
    Task<ImageUtil?> GetImage(string id);
    Task DeleteImage(string id);
}