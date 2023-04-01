using System.Diagnostics;
using ChatApplication.Exceptions;
using ChatApplication.Services;
using ChatApplication.Storage;
using ChatApplication.Web.Dtos;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;

namespace ChatApplication.Controllers;


[ApiController]
[Route("api/[controller]")]

public class ProfileController : ControllerBase

{
    private readonly IProfileService _profileService; //does the logic
    private readonly ILogger<ProfileController> _logger; //logs the errors

    private readonly TelemetryClient _telemetryClient; //tracks events and metrics
    //Single responsibility principle
    
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
            Profile profile;
            var stopWatch = Stopwatch.StartNew();
            profile = await _profileService.GetProfile(username);
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
                return CreatedAtAction(nameof(GetProfile), new { username = profile.username },
                    profile);
            }
            catch (ImageNotFoundException e)
            {
                return BadRequest(
                    $"There are no corresponding images for the profile with username: {profile.username}");
            }
            catch (ProfileAlreadyExistsException e)
            {
                return Conflict($"A profile with username {profile.username} already exists");
            }
        }
    }
}