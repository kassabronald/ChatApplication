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
        UnixTime timeSent;
        var message = new Message(messageRequest.messageId, messageRequest.senderUsername,
            messageRequest.messageContent, conversationId);
        try
        {
            timeSent = await _conversationService.AddMessage(message);
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
        return Created($"http://localhost/Conversations/conversations/{conversationId}/messages", new MessageResponse(timeSent.timestamp));
        
    }
    

    [HttpPost("conversations")]
    public async Task<ActionResult<ConversationResponse>> CreateConversation(ConversationRequest conversationRequest)
    {
        if (String.IsNullOrWhiteSpace(conversationRequest.conversationId))
        {
            return BadRequest($"Conversation id is missing");
        }
        if (conversationRequest.participants == null || conversationRequest.participants.Length == 0)
        {
            return BadRequest($"Participants for the conversation with id : {conversationRequest.conversationId} are missing");
        }
        var conversation = new Conversation(conversationRequest.conversationId, conversationRequest.participants);
        try
        {
            await _conversationService.CreateConversation(conversation);
        }
        catch (ConversationAlreadyExistsException)
        {
            return Conflict($"A conversation with id : {conversation.conversationId} already exists ");
        }
        catch (Exception)
        {
            return BadRequest("Bad request");
        }
        return Created($"conversations/{conversation.conversationId}", new ConversationResponse(conversation.conversationId));
    }
    
    
    
}