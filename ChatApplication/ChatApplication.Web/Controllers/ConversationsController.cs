using System.Diagnostics;
using System.Net;
using ChatApplication.Exceptions;
using ChatApplication.Services;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.Controllers;



[ApiController]
[Route("[controller]")]

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

    [HttpPost("conversations/{conversationId}/messages")]
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
                
                return Created("http://localhost/Conversations/conversations/{conversationId}/messages", 
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

    
    [HttpPost("conversations")]
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
    
    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<GetConversationMessagesResponse> GetConversationMessages(string conversationId, long lastMessageTime=0, 
        string continuationToken = "", int limit = 50)
    {
        var stopWatch = new Stopwatch();
        var messageAndToken = await _conversationService.GetConversationMessages(conversationId, limit, continuationToken, lastMessageTime);
        _telemetryClient.TrackMetric("ConversationService.GetConversationMessages.Time", stopWatch.ElapsedMilliseconds);
        var response = new GetConversationMessagesResponse("ok", messageAndToken.messages);
        return response; //TODO: Change this to the correct url.
    }
    
    
    
}