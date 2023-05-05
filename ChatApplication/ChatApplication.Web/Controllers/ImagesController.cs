using System.Diagnostics;
using ChatApplication.Exceptions;
using ChatApplication.Services;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Linq;

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
        try
        {
            var stopWatch = Stopwatch.StartNew();
            var image = await _imageService.GetImage(id);
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
        if (request.File.Length == 0) return new UploadImageResponse("");
        using (_logger.BeginScope("Uploading Image with name {Name}", request.File.FileName))
        {
            using var stream = new MemoryStream();
            await request.File.CopyToAsync(stream);
            if (stream.Length == 0)
            {
                return BadRequest($"File {request.File.FileName} is empty");
            }

            var stopWatch = Stopwatch.StartNew();
            var id = await _imageService.AddImage(stream, request.File.ContentType);
            _logger.LogInformation("Image added with id {Id}", id);
            _telemetryClient.TrackMetric("ImageService.AddImage.Time", stopWatch.ElapsedMilliseconds);
            _telemetryClient.TrackEvent("ImageAdded");
            return CreatedAtAction(nameof(DownloadImage), new { id }, new UploadImageResponse(id));
        }

    }
}