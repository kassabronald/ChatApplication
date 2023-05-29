using System.Diagnostics;
using ChatApplication.Exceptions;
using ChatApplication.Services;
using ChatApplication.Web.Dtos;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.Controllers;


[ApiController]
[Route("api/[controller]")]

public class ProfileController : ControllerBase

{
    private readonly IProfileService _profileService; 
    private readonly ILogger<ProfileController> _logger; 
    private readonly TelemetryClient _telemetryClient; 
    
    public ProfileController(IProfileService profileService, ILogger<ProfileController> logger, TelemetryClient telemetryClient)
    {
        _profileService = profileService;
        _logger = logger;
        _telemetryClient = telemetryClient;
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<Profile>> GetProfile(string username)
    {
        try
        {
            var stopWatch = Stopwatch.StartNew();
            var profile = await _profileService.GetProfile(username);
            _telemetryClient.TrackMetric("ProfileService.GetProfile.Time", stopWatch.ElapsedMilliseconds);
            return Ok(profile);
        }
        catch (ProfileNotFoundException)
        {
            return NotFound($"A User with username {username} was not found");
        }
    }

    [HttpPost]
    public async Task<ActionResult<Profile>> AddProfile(Profile profile)
    {
        using (_logger.BeginScope("Creating profile {Profile}", profile))
        {
            try
            {
                var stopWatch = Stopwatch.StartNew();
                await _profileService.AddProfile(profile);
                _telemetryClient.TrackMetric("ProfileService.AddProfile.Time", stopWatch.ElapsedMilliseconds);
                _telemetryClient.TrackEvent("ProfileAdded");
                _logger.LogInformation("Profile created");
                return CreatedAtAction(nameof(GetProfile), new { username = profile.Username },
                    profile);
            }
            catch (ImageNotFoundException)
            {
                return BadRequest(
                    $"There are no corresponding images for the profile with username: {profile.Username}");
            }
            catch (ProfileAlreadyExistsException)
            {
                return Conflict($"A profile with username {profile.Username} already exists");
            }
        }
    }
}