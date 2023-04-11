using ChatApplication.Exceptions;
using ChatApplication.Exceptions.ConversationParticipantsExceptions;
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
    public ConversationService(IMessageStore messageStore, IConversationStore conversationStore, IProfileStore profileStore)
    {
        _messageStore = messageStore;
        _conversationStore = conversationStore;
        _profileStore = profileStore;
    }
    public async Task AddMessage(Message message)
    {
        var conversation = await _conversationStore.GetUserConversation(message.SenderUsername, message.ConversationId);
        await _conversationStore.UpdateConversationLastMessageTime(conversation, message.CreatedUnixTime);
        try
        {
            await _messageStore.AddMessage(message);
        }
        catch (MessageAlreadyExistsException)
        { }
    }
    
    public async Task<string> StartConversation(StartConversationParameters parameters)
    {
        
        checkIfValidParticipants(parameters.participants, parameters.senderUsername);
        var sortedParticipants = new List<string>(parameters.participants);
        sortedParticipants.Sort();
        var id = sortedParticipants.Aggregate("", (current, participantUsername) => current + ("_" + participantUsername));
        
        var message = new Message(parameters.messageId, parameters.senderUsername, parameters.messageContent, parameters.createdTime, id);
        
        try
        {
            await _messageStore.AddMessage(message);
        }
        catch (MessageAlreadyExistsException)
        {
        }

        var participantsProfile =
            await Task.WhenAll(sortedParticipants.Select(participant => _profileStore.GetProfile(participant)));
        var userConversations = sortedParticipants.Select(participantUsername =>
        {
            var recipients = new List<Profile>(participantsProfile);
            recipients.Remove(Array.Find(participantsProfile, x => x.Username == participantUsername)!);
            return new UserConversation(id, recipients, parameters.createdTime, participantUsername);
        }).ToList();

        try
        {
            await Task.WhenAll(userConversations.Select(conversation =>
                _conversationStore.CreateUserConversation(conversation)));
        }
        catch (ConversationAlreadyExistsException)
        {
            return id;
        }

        //TODO: After PR1 handle possible errors
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
    
    private void checkIfValidParticipants(IReadOnlyCollection<string> participants, string senderUsername)
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

}