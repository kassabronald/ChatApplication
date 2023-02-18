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
        byte[] image = await _imageStore.GetImage(id);
        if (image.isnull)
        {
            return NotFound($"An image with id {id} was not found");
        }
        return File(image, "image/png");
    }
    
    
}