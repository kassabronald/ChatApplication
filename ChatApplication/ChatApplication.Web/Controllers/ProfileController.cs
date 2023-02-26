using ChatApplication.Services;
using ChatApplication.Storage;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.Controllers;


[ApiController]
[Route("[controller]")]

public class ProfileController : ControllerBase

{
    private readonly IProfileService _profileService; //does the logic
    //Single responsibility principle
    
    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }
    
    
    [HttpGet("{username}")]
    public async Task<ActionResult<Profile>> GetProfile(string username)
    {
        
        var profile = await _profileService.GetProfile(username);
        if (profile == null)
        {
            return NotFound($"A User with username {username} was not found");
        }
        return Ok(profile);
    }

    [HttpPost]
    public async Task<ActionResult<Profile>> AddProfile(Profile profile)
    {
        var existingProfile = await _profileService.GetProfile(profile.username);
        if (existingProfile != null)
        {
            return Conflict($"A user with username {profile.username} already exists");
        }
        await _profileService.AddProfile(profile);
        return CreatedAtAction(nameof(GetProfile), new {username = profile.username},
            profile);

    }
}