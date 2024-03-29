using System.Diagnostics;
using System.Net;
using ChatApplication.Exceptions;
using ChatApplication.Exceptions.ConversationParticipantsExceptions;
using ChatApplication.Services;
using ChatApplication.Web.Dtos;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(IConversationService conversationService, TelemetryClient telemetryClient,
        ILogger<ConversationsController> logger)
    {
        _conversationService = conversationService;
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    [HttpPost("{conversationId}/messages")]
    public async Task<ActionResult<SendMessageResponse?>> AddMessage(SendMessageRequest sendMessageRequest,
        string conversationId)
    {
        using (_logger.BeginScope("Adding message {Message} to conversation {ConversationId}", sendMessageRequest,
                   conversationId))
        {
            if (!sendMessageRequest.IsValid(out var error))
            {
                return BadRequest(error);
            }
            
            var time = DateTimeOffset.UtcNow;
            Console.WriteLine("time of added message is "+time.ToUnixTimeMilliseconds());
            var message = new Message(sendMessageRequest.Id, sendMessageRequest.SenderUsername, conversationId,
                sendMessageRequest.Text, time.ToUnixTimeMilliseconds());

            try
            {
                var stopWatch = new Stopwatch();
                await _conversationService.EnqueueAddMessage(message);
                _telemetryClient.TrackMetric("ConversationService.EnqueueAddMessage.Time",
                    stopWatch.ElapsedMilliseconds);
                _telemetryClient.TrackEvent("MessageAdded");
                _logger.LogInformation("Message added");

                return CreatedAtAction(nameof(GetMessages), new { conversationId },
                    new SendMessageResponse(message.CreatedUnixTime));
            }
            catch (ConversationNotFoundException)
            {
                return NotFound($"A conversation with id : {conversationId} was not found");
            }
            catch (MessageAlreadyExistsException)
            {
                return Conflict($"A message with id : {message.MessageId} already exists ");
            }
        }
    }

    [HttpPost]
    public async Task<ActionResult<StartConversationResponse>> StartConversation(
        StartConversationRequest conversationRequest)
    {
        if (!conversationRequest.IsValid(out var error))
        {
            return BadRequest(error);
        }

        try
        {
            var createdTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Console.WriteLine("Created time is: " + createdTime);
            var stopWatch = new Stopwatch();
            var startConversationParameters = new StartConversationParameters(
                conversationRequest.FirstMessage.Id, conversationRequest.FirstMessage.SenderUsername,
                conversationRequest.FirstMessage.Text, createdTime, conversationRequest.Participants);

            var id = await _conversationService.EnqueueStartConversation(startConversationParameters);

            _telemetryClient.TrackMetric("ConversationService.EnqueueStartConversation.Time", stopWatch.ElapsedMilliseconds);
            _telemetryClient.TrackEvent("ConversationStarted");
            _logger.LogInformation("Conversation started with id {conversationId}", id);
            
            var response = new StartConversationResponse(id, createdTime, conversationRequest.Participants);

            return CreatedAtAction(nameof(GetConversations),
                new { username = conversationRequest.FirstMessage.SenderUsername }, response);
        }
        catch (Exception e) when (e is SenderNotFoundException or DuplicateParticipantException)
        {
            return BadRequest(e.Message);
        }
        catch (ProfileNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (MessageAlreadyExistsException e)
        {
            return Conflict(e.Message);
        }
    }

    [HttpGet("{conversationId}/messages")]
    public async Task<GetMessagesResponse> GetMessages(string conversationId, int limit = 50,
        string continuationToken = "", long lastSeenMessageTime = 0)
    {
        var stopWatch = new Stopwatch();
        var getMessagesParameters =
            new GetMessagesParameters(conversationId, limit, continuationToken, lastSeenMessageTime);

        var getMessagesResult = await _conversationService.GetMessages(getMessagesParameters);
        _telemetryClient.TrackMetric("ConversationService.GetConversationMessages.Time", stopWatch.ElapsedMilliseconds);
        var nextUri = "";

        if (getMessagesResult.ContinuationToken != null)
        {
            nextUri = $"/api/Conversations/{conversationId}/messages?limit={limit}&continuationToken={WebUtility.UrlEncode(getMessagesResult.ContinuationToken)}&lastSeenMessageTime={lastSeenMessageTime}";
        }

        var response = new GetMessagesResponse(getMessagesResult.Messages, nextUri);

        return response;
    }

    [HttpGet]
    public async Task<ActionResult<GetConversationsResponse>> GetConversations(string username, int limit = 50,
        string continuationToken = "", long lastSeenConversationTime = 0)
    {
        var stopWatch = new Stopwatch();
        var getConversationsParameters =
            new GetConversationsParameters(username, limit, continuationToken, lastSeenConversationTime);

        var getConversationsResult = await _conversationService.GetConversations(getConversationsParameters);
        _telemetryClient.TrackMetric("ConversationService.GetConversations.Time", stopWatch.ElapsedMilliseconds);
        
        var nextUri = "";

        if (getConversationsResult.ContinuationToken != null)
        {
            nextUri = $"/api/Conversations?username={username}&limit={limit}&continuationToken={WebUtility.UrlEncode(getConversationsResult.ContinuationToken)}&lastSeenConversationTime={lastSeenConversationTime}";
        }
        
        var response = new GetConversationsResponse(getConversationsResult.ToMetadata(username), nextUri);

        return response;
    }
}