using System.Diagnostics;
using System.Net;
using ChatApplication.Exceptions;
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
            var time = DateTimeOffset.UtcNow;
            //TODO: After PR1, use custom serializer.
            var message = new Message(sendMessageRequest.Id, sendMessageRequest.SenderUsername,
                sendMessageRequest.Text, time.ToUnixTimeSeconds(), conversationId);

            try
            {
                var stopWatch = new Stopwatch();
                await _conversationService.AddMessage(message);
                _telemetryClient.TrackMetric("ConversationService.AddMessage.Time", stopWatch.ElapsedMilliseconds);
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
            catch (Exception)
            {
                return BadRequest("Bad request");
            }
        }
    }

    [HttpPost]
    public async Task<ActionResult<StartConversationResponse>> CreateConversation(
        StartConversationRequest conversationRequest)
    {
        var numberOfParticipants = conversationRequest.Participants.Count;

        if (numberOfParticipants < 2)
            return BadRequest(
                $"A conversation must have at least 2 participants but only {numberOfParticipants} were provided");

        try
        {
            var createdTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var stopWatch = new Stopwatch();
            StartConversationParameters startConversationParameters = new StartConversationParameters(
                conversationRequest.FirstSendMessage.Id, conversationRequest.FirstSendMessage.SenderUsername,
                conversationRequest.FirstSendMessage.Text, createdTime, conversationRequest.Participants);
            
            var id = await _conversationService.StartConversation(startConversationParameters);

            _telemetryClient.TrackMetric("ConversationService.StartConversation.Time", stopWatch.ElapsedMilliseconds);
            _telemetryClient.TrackEvent("ConversationStarted");
            _logger.LogInformation("Conversation started with id {conversationId}", id);
            var response = new StartConversationResponse(id, createdTime);

            return CreatedAtAction(nameof(GetConversations),
                new { username = conversationRequest.FirstSendMessage.SenderUsername }, response);
        }
        catch (ProfileNotFoundException e)
        {
            return BadRequest(
                $"The username : {e.Username} is the sender and was not found in the participants list or does not exist");
        }
        catch (ConversationAlreadyExistsException e)
        {
            return Conflict(e.Message);
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
        var decodedContinuationToken = WebUtility.UrlDecode(continuationToken);
        var getMesaagesParameters =
            new GetMessagesParameters(conversationId, limit, decodedContinuationToken, lastSeenMessageTime);

        var getMessagesResult = await _conversationService.GetMessages(getMesaagesParameters);
        _telemetryClient.TrackMetric("ConversationService.GetConversationMessages.Time", stopWatch.ElapsedMilliseconds);
        var nextUri =
            $"/api/Conversations/{conversationId}/messages?&limit={limit}&continuationToken={WebUtility.UrlEncode(getMessagesResult.ContinuationToken)}&lastSeenMessageTime={lastSeenMessageTime}";

        var response = new GetMessagesResponse(getMessagesResult.Messages, nextUri);

        return response;
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<GetConversationsResponse>> GetConversations(string username, int limit = 50,
        string continuationToken = "", long lastSeenConversationTime = 0)
    {
        var stopWatch = new Stopwatch();
        var decodedContinuationToken = WebUtility.UrlDecode(continuationToken);
        var getConversationsParameters =
            new GetConversationsParameters(username, limit, decodedContinuationToken, lastSeenConversationTime);

        var getConversationsResult = await _conversationService.GetConversations(getConversationsParameters);
        _telemetryClient.TrackMetric("ConversationService.GetConversations.Time", stopWatch.ElapsedMilliseconds);
        var nextUri =
            $"/api/Conversations/{username}?&limit={limit}&continuationToken={WebUtility.UrlEncode(getConversationsResult.ContinuationToken)}&lastSeenConversationTime={lastSeenConversationTime}";

        var response = new GetConversationsResponse(getConversationsResult.ToMetadata(), nextUri);

        return response;
    }
}