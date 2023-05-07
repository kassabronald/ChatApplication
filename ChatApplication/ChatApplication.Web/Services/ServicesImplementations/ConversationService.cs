using ChatApplication.Exceptions;
using ChatApplication.Exceptions.ConversationParticipantsExceptions;
using ChatApplication.ServiceBus.Interfaces;
using ChatApplication.Storage;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Newtonsoft.Json;

namespace ChatApplication.Services;

public class ConversationService : IConversationService
{
    private readonly IMessageStore _messageStore;
    private readonly IConversationStore _conversationStore;
    private readonly IProfileStore _profileStore;
    private readonly IAddMessageServiceBusPublisher _addMessageServiceBusPublisher;
    private readonly IStartConversationServiceBusPublisher _startConversationServiceBusPublisher;
    public ConversationService(IMessageStore messageStore, IConversationStore conversationStore, IProfileStore profileStore, 
        IAddMessageServiceBusPublisher addMessageServiceBusPublisher, IStartConversationServiceBusPublisher startConversationServiceBusPublisher)
    {
        _messageStore = messageStore;
        _conversationStore = conversationStore;
        _profileStore = profileStore;
        _addMessageServiceBusPublisher = addMessageServiceBusPublisher;
        _startConversationServiceBusPublisher = startConversationServiceBusPublisher;
    }
    
    public async Task EnqueueAddMessage(Message message)
    {
        await _conversationStore.GetUserConversation(message.SenderUsername, message.ConversationId);

        try
        {
            await _messageStore.GetMessage(message.ConversationId, message.MessageId);
        }
        catch (MessageNotFoundException)
        {
            await _addMessageServiceBusPublisher.Send(message);
            return;
        }

        throw new MessageAlreadyExistsException($"Message with id {message.MessageId} already exists");
    }

    public async Task<string> EnqueueStartConversation(StartConversationParameters parameters)
    {
        
        CheckIfValidParticipants(parameters.participants, parameters.senderUsername);
        await Task.WhenAll(parameters.participants.Select(participant => _profileStore.GetProfile(participant)));

        var id = GenerateConversationId(parameters.participants);
        try
        {
            await _messageStore.GetMessage(id, parameters.messageId);
        }
        catch (MessageNotFoundException)
        {
            await _startConversationServiceBusPublisher.Send(parameters);
            return id;
        }

        throw new MessageAlreadyExistsException($"Message with id {parameters.messageId} already exists");


    }

    public async Task AddMessage(Message message)
    {
        var senderConversation = await _conversationStore.GetUserConversation(message.SenderUsername, message.ConversationId);
        await _conversationStore.UpdateConversationLastMessageTime(senderConversation, message.CreatedUnixTime);
        try
        {
            await _messageStore.AddMessage(message);
        }
        catch (MessageAlreadyExistsException)
        {
            return;
        }
        
    }
    
    public async Task<string> StartConversation(StartConversationParameters parameters)
    {
        var id = GenerateConversationId(parameters.participants);
        var participantsProfile =
            await Task.WhenAll(parameters.participants.Select(participant => _profileStore.GetProfile(participant)));
        
        var userConversations = parameters.participants.Select(participantUsername =>
        {
            var recipients = new List<Profile>(participantsProfile);
            recipients.Remove(Array.Find(participantsProfile, x => x.Username == participantUsername));
            return new UserConversation(id, recipients, parameters.createdTime, participantUsername);
        }).ToList();

        try
        {
            await Task.WhenAll(userConversations.Select(conversation =>
                _conversationStore.CreateUserConversation(conversation)));
        }
        catch (ConversationAlreadyExistsException)
        {
        }
        
        var message = new Message(parameters.messageId, parameters.senderUsername, id, parameters.messageContent, parameters.createdTime);

        try
        { 
            await _messageStore.AddMessage(message); 
        }
        catch (MessageAlreadyExistsException)
        {
        }  
        
        return id;
    }
    
    public async Task<GetMessagesResult > GetMessages(GetMessagesParameters parameters)
    {
        return await _messageStore.GetMessages(parameters);
    }
    
    public async Task<GetConversationsResult> GetConversations(GetConversationsParameters parameters)
    {
        return await _conversationStore.GetConversations(parameters);
    }
    
    private static void CheckIfValidParticipants(IReadOnlyCollection<string> participants, string senderUsername)
    {
        var foundSenderUsername = participants.Aggregate(false, (current, participant) => current || participant == senderUsername);
        if (!foundSenderUsername)
        {
            throw new SenderNotFoundException($"Sender username {senderUsername} not found in participants");
        }
        var duplicates = participants.GroupBy(p => p).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Any())
        {
            throw new DuplicateParticipantException($"Participant(s) {string.Join(", ", duplicates)} is/are duplicated");
        }
    }
    
    private static string GenerateConversationId(List<string> participants)
    {
        participants.Sort();
        return participants.Aggregate("", (current, participant) => current + ("_" + participant));
    }

}