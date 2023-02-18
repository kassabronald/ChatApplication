using ChatApplication.Storage;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.Controllers;


[ApiController]
[Route("[controller]")]

public class ProfileController : ControllerBase

{
    private readonly IProfileStore _profileStore;
    
    public ProfileController(IProfileStore profileStore)
    {
        _profileStore = profileStore;
    }
    
    
    [HttpGet("{username}")]
    public async Task<ActionResult<Profile>> GetProfile(string username)
    {
        var profile = await _profileStore.GetProfile(username);
        if (profile == null)
        {
            return NotFound($"A User with username {username} was not found");
        }
        return Ok(profile);
    }

    [HttpPost]
    public async Task<ActionResult<Profile>> AddProfile(Profile profile)
    {
        var existingProfile = await _profileStore.GetProfile(profile.username);
        if (existingProfile != null)
        {
            return Conflict($"A user with username {profile.username} already exists");
        }
        await _profileStore.AddProfile(profile);
        return CreatedAtAction(nameof(GetProfile), new {username = profile.username},
            profile);

    }
}