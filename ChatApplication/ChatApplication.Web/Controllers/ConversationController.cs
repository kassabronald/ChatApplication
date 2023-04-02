using System.Diagnostics;
using System.Net;
using System.Web;
using ChatApplication.Exceptions;
using ChatApplication.Services;
using ChatApplication.Utils;
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

    public ConversationsController(IConversationService conversationService, TelemetryClient telemetryClient, ILogger<ConversationsController> logger)
    {
        _conversationService = conversationService;
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    [HttpPost("{conversationId}/messages")]
    public async Task<ActionResult<MessageResponse?>> AddMessage(MessageRequest messageRequest, string conversationId)
    {
        using (_logger.BeginScope("Adding message {Message} to conversation {ConversationId}", messageRequest,
                   conversationId))
        {
            DateTimeOffset time = DateTimeOffset.UtcNow;
            //TODO: After PR1, use custom serializer.
            var message = new Message(messageRequest.messageId, messageRequest.senderUsername,
                messageRequest.messageContent, time.ToUnixTimeSeconds(), conversationId);
            try
            {
                var stopWatch = new Stopwatch();
                await _conversationService.AddMessage(message);
                _telemetryClient.TrackMetric("ConversationService.AddMessage.Time", stopWatch.ElapsedMilliseconds);
                _telemetryClient.TrackEvent("MessageAdded");
                _logger.LogInformation("Message added");
                //TODO: Change when we create get method.
                
                return Created($"http://localhost/Conversations/conversations/{conversationId}/messages", 
                    new MessageResponse(message.createdUnixTime));
                // return CreatedAtAction(nameof(GetConversationMessages), conversationId, 
                //     new MessageResponse(message.createdUnixTime));
            }
            catch (ConversationNotFoundException)
            {
                return NotFound($"A conversation with id : {conversationId} was not found");
            }
            catch (MessageAlreadyExistsException)
            {
                return Conflict($"A message with id : {message.messageId} already exists ");
            }
            catch (Exception)
            {
                return BadRequest("Bad request");
            }
        }
    }

    
    [HttpPost]
    public async Task<ActionResult<StartConversationResponse>> CreateConversation(StartConversationRequest conversationRequest)
    {
        var numberOfParticipants = conversationRequest.Participants.Count;
        if (numberOfParticipants < 2)
        {
            return BadRequest($"A conversation must have at least 2 participants but only {numberOfParticipants} were provided");
        }
        
        try
        {
            long createdTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string id="";
            var stopWatch = new Stopwatch();
            id = await _conversationService.StartConversation(conversationRequest.FirstMessage.messageId,
                conversationRequest.FirstMessage.senderUsername,
                conversationRequest.FirstMessage.messageContent, createdTime,
                conversationRequest.Participants);
            _telemetryClient.TrackMetric("ConversationService.StartConversation.Time", stopWatch.ElapsedMilliseconds);
            _telemetryClient.TrackEvent("ConversationStarted");
            _logger.LogInformation("Conversation started with id {conversationId}", id);
            var response = new StartConversationResponse(id, createdTime);
            //TODO: Change this to the correct url.
            return Created($"http://localhost/Conversations/conversations/{id}", response);
        }
        catch (ProfileNotFoundException e)
        {
            return BadRequest($"The username : {e.Username} is the sender and was not found in the participants list or does not exist");
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
    public async Task<GetConversationMessagesResponse> GetConversationMessages(string conversationId, int limit = 50, 
        string continuationToken = "", long lastSeenMessageTime=0)
    {
        var stopWatch = new Stopwatch();
        var messageAndToken = await _conversationService.GetConversationMessages(conversationId, limit, continuationToken, lastSeenMessageTime);
        _telemetryClient.TrackMetric("ConversationService.GetConversationMessages.Time", stopWatch.ElapsedMilliseconds);
        var nextUri =
            $"/api/Conversations/{conversationId}/messages?&limit={limit}&continuationToken={messageAndToken.continuationToken}&lastSeenMessageTime={lastSeenMessageTime}";
        var response = new GetConversationMessagesResponse(messageAndToken.messages, nextUri);
        return response; //TODO: Change this to the correct url.
    }
    
    [HttpGet("{username}")]
    
    public async Task<ActionResult<GetAllConversationsResponse>> GetAllConversations(string username, int limit = 50, string continuationToken = "", long lastSeenConversationTime=0)
    {
        var stopWatch = new Stopwatch();
        var conversationsAndToken = await _conversationService.GetAllConversations(username, limit, continuationToken, lastSeenConversationTime);
        _telemetryClient.TrackMetric("ConversationService.GetConversations.Time", stopWatch.ElapsedMilliseconds);
        string nextUri =
            $"/api/Conversations/{username}?&limit={limit}&continuationToken={conversationsAndToken.continuationToken}&lastSeenConversationTime={lastSeenConversationTime}";
        var response = new GetAllConversationsResponse(conversationsAndToken.ToMetadata(), nextUri);
        return response; //TODO: Change this to the correct url.
    }
    
    
    
}