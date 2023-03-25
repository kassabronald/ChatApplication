using System.Net;
using ChatApplication.Exceptions;
using ChatApplication.Services;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.Controllers;



[ApiController]
[Route("[controller]")]

public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;

    public ConversationsController(IConversationService conversationService)
    {
        _conversationService = conversationService;
    }
    
    [HttpPost("conversations/{conversationId}/messages")]
    public async Task<ActionResult<MessageResponse?>> AddMessage(MessageRequest messageRequest, string conversationId)
    {
        DateTimeOffset time = DateTimeOffset.UtcNow;
        var message = new Message(messageRequest.messageId, messageRequest.senderUsername,
            messageRequest.messageContent,time.ToUnixTimeSeconds(), conversationId);
        try
        {
            await _conversationService.AddMessage(message);
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
        //TODO: Change when we create get method.
        return Created($"http://localhost/Conversations/conversations/{conversationId}/messages", new MessageResponse(message.createdUnixTime));
    }
    
    [HttpPost("conversations")]
    public async Task<ActionResult<StartConversationResponse>> CreateConversation(StartConversationRequest conversationRequest)
    {
        var numberOfParticipants = conversationRequest.Participants.Count;
        if (numberOfParticipants < 2)
        {
            return BadRequest($"A conversation must have at least 2 participants but only {numberOfParticipants} were provided");
        }
        var foundSenderUsername = false;
        foreach (var participant in conversationRequest.Participants)
        {
            foundSenderUsername= foundSenderUsername || participant == conversationRequest.FirstMessage.senderUsername;
        }
        if(!foundSenderUsername)
        {
            return BadRequest($"The sender username : {conversationRequest.FirstMessage.senderUsername} was not found in the participants list");
        }
        long createdTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string id="";
        try
        {
            id = await _conversationService.StartConversation(conversationRequest.FirstMessage.messageId,
                conversationRequest.FirstMessage.senderUsername,
                conversationRequest.FirstMessage.messageContent, createdTime,
                conversationRequest.Participants);
        }
        catch (ProfileNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (ConversationAlreadyExistsException e)
        {
            return Conflict(e.Message);
        }
        catch (MessageAlreadyExistsException e)
        {
            return Conflict(e.Message);
        }
        var response = new StartConversationResponse(id, createdTime);
        return Created($"http://localhost/Conversations/conversations/{id}", response);
    }
    
    [HttpGet("conversations/{conversationId}/messages")]
    
    public async Task<GetConversationMessagesResponse> GetConversationMessages(string conversationId, string? continuationToken
        ,long? lastMessageTime, int? limit = 50)
    {
        var messages = await _conversationService.GetConversationMessages(conversationId);
        var response = new GetConversationMessagesResponse("ok", messages);
        return response;
    }
    
    
    
}