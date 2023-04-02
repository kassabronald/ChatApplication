using System.Diagnostics;
using ChatApplication.Exceptions;
using ChatApplication.Services;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly IImageService _imageService;
    private readonly ILogger<ImagesController> _logger;
    private readonly TelemetryClient _telemetryClient;
    
    public ImagesController(IImageService imageService, ILogger<ImagesController> logger, TelemetryClient telemetryClient)
    {
        _imageService = imageService;
        _logger = logger;
        _telemetryClient = telemetryClient;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult?> DownloadImage(string id)
    {
        ImageUtil? image;
        try
        {
            var stopWatch = Stopwatch.StartNew();
            image = await _imageService.GetImage(id);
            _telemetryClient.TrackMetric("ImageService.GetImage.Time", stopWatch.ElapsedMilliseconds);
            return new FileContentResult(image!.ImageData, image.ContentType);
        }
        catch (ArgumentException)
        {
            return BadRequest($"Invalid image id : {id}");
        }
        catch (ImageNotFoundException)
        {
            return NotFound($"Image with id :{id} not found");
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
            var stopWatch = Stopwatch.StartNew();
            var id = await _imageService.AddImage(stream, request.File.ContentType);
            _telemetryClient.TrackMetric("ImageService.AddImage.Time", stopWatch.ElapsedMilliseconds);
            _telemetryClient.TrackEvent("ImageAdded");
            return CreatedAtAction(nameof(DownloadImage), new { id }, new UploadImageResponse(id));
        }
    }
}