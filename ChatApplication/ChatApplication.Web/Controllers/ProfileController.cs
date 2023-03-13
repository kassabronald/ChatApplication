using ChatApplication.Exceptions;
using ChatApplication.Services;
using ChatApplication.Storage;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;

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
        Profile profile;
        try
        {
            profile = await _profileService.GetProfile(username);
        }
        catch (ProfileNotFoundException)
        {
            return NotFound($"A User with username {username} was not found"); 
        }
        return Ok(profile);
    }

    [HttpPost]
    public async Task<ActionResult<Profile>> AddProfile(Profile profile)
    {
        try
        {
            await _profileService.AddProfile(profile);
        }
        catch (ImageNotFoundException e)
        {
            return BadRequest($"There are no corresponding images for the profile with username: {profile.username}");
        }
        catch (ProfileAlreadyExistsException e)
        {
            return Conflict($"A profile with username {profile.username} already exists");
        }
        return CreatedAtAction(nameof(GetProfile), new {username = profile.username},
            profile);
    }
}