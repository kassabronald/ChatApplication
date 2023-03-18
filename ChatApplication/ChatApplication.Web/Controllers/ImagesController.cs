using ChatApplication.Exceptions;
using ChatApplication.Services;
using ChatApplication.Storage;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.Controllers;

[ApiController]
[Route("[controller]")]
public class ImagesController : ControllerBase
{
    private readonly IImageService _imageService;
    private readonly ILogger<ImagesController> _logger;
    
    public ImagesController(IImageService imageService, ILogger<ImagesController> logger)
    {
        _imageService = imageService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult?> DownloadImage(string id)
    {
        using (_logger.BeginScope("Downloading Image with id {Id}", id))
        {
            ImageUtil? image;
            try
            {
                image = await _imageService.GetImage(id);
            }
            catch (ArgumentException e)
            {
                return BadRequest($"Invalid image id : {id}");
            }
            catch (ImageNotFoundException e)
            {
                return NotFound($"Image with id :{id} not found");
            }
            _logger.LogInformation("Image found");
            return new FileContentResult(image._imageData, image._contentType);
        }
    }

    [HttpPost]
    public async Task<ActionResult<UploadImageResponse>> UploadImage([FromForm] UploadImageRequest request)
    {
        using (_logger.BeginScope("Uploading Image with name {Name}", request.File.FileName))
        {
            string contentType = request.File.ContentType.ToLower();
            if (contentType != "image/jpeg" && contentType != "image/png" && contentType != "image/jpg")
            {
                return BadRequest($"Only JPEG and PNG and JPG images are supported, not {contentType}");
            }

            using var stream = new MemoryStream();
            await request.File.CopyToAsync(stream);
            if (stream.Length == 0)
            {
                return BadRequest($"File {request.File.FileName} is empty");
            }

            var id = await _imageService.AddImage(stream, request.File.ContentType);
            return CreatedAtAction(nameof(DownloadImage), new { id }, new UploadImageResponse(id));
        }
    }
}