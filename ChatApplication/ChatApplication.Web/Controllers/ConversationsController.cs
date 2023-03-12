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
    


    // [HttpPost("conversations")]
    // public async Task<ActionResult<StartConversationResponse>> CreateConversation(StartConversationRequest conversationRequest)
    // {
    //     return Ok();
    // }
    
    
    
}