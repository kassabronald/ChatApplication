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
    public async Task<ActionResult<FileContentResult>> DownloadImage(string id)
    {
        var image = await _imageStore.GetImage(id);
        if (image == null)
        {
            return NotFound($"An image with id {id} was not found");
        }
        return File(image, "image/png");
    }
    
    [HttpPost]
    public async Task<ActionResult<UploadImageResponse>> UploadImage([FromForm] UploadImageRequest request)
    {
        //add image to storage
        using var stream = new MemoryStream();
        await request.File.CopyToAsync(stream);
        var id = await _imageStore.AddImage(request.File.FileName, stream);
        if (id == null)
        {
            return BadRequest("Failed to upload image");
        }
        return Ok(new UploadImageResponse(id));
    }
    
    
}