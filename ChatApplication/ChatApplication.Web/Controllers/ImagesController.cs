using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.Controllers;

public class ImagesController
{
    [ApiController]
    [Route("[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly IImageStore _imageStore;

        public ImagesController(IImageStore imageStore)
        {
            _imageStore = imageStore;
        }

        [HttpGet("{username}")]
        public async Task<ActionResult<Image>> GetImage(string username)
        {
            var image = await _imageStore.GetImage(username);
            if (image == null)
            {
                return NotFound($"An image for user {username} was not found");
            }

            return Ok(image);
        }

        [HttpPost]
        public async Task<ActionResult<Image>> AddImage(Image image)
        {
            var existingImage = await _imageStore.GetImage(image.username);
            if (existingImage != null)
            {
                return Conflict($"An image for user {image.username} already exists");
            }

            await _imageStore.AddImage(image);
            return CreatedAtAction(nameof(GetImage), new {username = image.username},
                image);
        }

        [HttpPut("{username?}")]
        public async Task<ActionResult<Image>> UpdateImage(string username, PutImageRequest request)
        {
            var existingImage = await _imageStore.GetImage(username);
            if (existingImage == null)
            {
                return NotFound($"An image for user {username} was not found");
            }

            var image = new Image(username, request.image);
            await _imageStore.UpsertImage(image);
            return Ok(image);
        }
    }
    
    
}