using ChatApplication.Storage;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.Controllers;

[ApiController]
[Route("[controller]")]
public class ImagesController : ControllerBase
{
    private readonly IImageStore _imageStore;
    
    public ImagesController(IImageStore imageStore)
    {
        _imageStore = imageStore;
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> DownloadImage(string id)
    {
        var image = await _imageStore.GetImage(id);
        if (image == null)
        {
            return NotFound("Image not found");
        }
        return image;


    }
    
    [HttpPost]
    public async Task<ActionResult<UploadImageResponse>> UploadImage([FromForm] UploadImageRequest request)
    {
        string contentType = request.File.ContentType.ToLower();
        
        if (contentType != "image/jpeg" && contentType != "image/png" && contentType != "image/jpg")
        {
            return BadRequest("Only JPEG and PNG and JPG images are supported");
        }
        
        
        
        using var stream = new MemoryStream();
        await request.File.CopyToAsync(stream);
        var id = Guid.NewGuid().ToString();
        await _imageStore.AddImage(id, stream, request.File.ContentType);
        return CreatedAtAction(nameof(DownloadImage), new {id}, new UploadImageResponse(id));
    }
    
    
}