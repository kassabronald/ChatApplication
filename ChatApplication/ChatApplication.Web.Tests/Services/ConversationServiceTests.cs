using ChatApplication.Exceptions;
using ChatApplication.Services;
using ChatApplication.Storage;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;
using Moq;

namespace ChatApplication.Web.Tests.Services;

public class ConversationServiceTests
{
    private readonly Mock<IMessageStore> _messageStoreMock = new();
    private readonly Mock<IConversationStore> _conversationStoreMock = new();
    private readonly Mock<IProfileStore> _profileStoreMock = new();
    private readonly ConversationService _conversationService;
    public ConversationServiceTests()
    {
        _conversationService = new ConversationService(_messageStoreMock.Object, _conversationStoreMock.Object, _profileStoreMock.Object);
    }

    [Fact]

    public async Task AddMessage()
    {
        var message = new Message("123", "Jad", "bro got W rizz", 1000,"_jad_rizz");
        var senderProfile = new Profile("Jad", "Jad", "Haddad", "12345");
        var receiverProfile = new Profile("rizz", "Rizwan", "Haddad", "123456");
        var participants = new List<Profile> { senderProfile, receiverProfile };
        var senderConversation = new Conversation("_jad_rizz", participants, 100000, senderProfile.username);
        var receiverConversation = new Conversation("_jad_rizz", participants, 100000, receiverProfile.username);
        _conversationStoreMock.Setup(m => m.GetConversation(senderProfile.username, senderConversation.conversationId)).ReturnsAsync(senderConversation);
        _conversationStoreMock.Setup(m => m.GetConversation(receiverProfile.username, receiverConversation.conversationId)).ReturnsAsync(receiverConversation);
        await _conversationService.AddMessage(message);
        _conversationStoreMock.Verify(mock => mock.GetConversation(senderProfile.username, senderConversation.conversationId), Times.Once);
        _conversationStoreMock.Verify(mock => mock.GetConversation(receiverProfile.username, receiverConversation.conversationId), Times.Once);
        _conversationStoreMock.Verify(mock => mock.ChangeConversationLastMessageTime(senderConversation, message.createdUnixTime), Times.Once);
        _conversationStoreMock.Verify(mock => mock.ChangeConversationLastMessageTime(receiverConversation, message.createdUnixTime), Times.Once);
        _messageStoreMock.Verify(mock => mock.AddMessage(message), Times.Once);
    }


    [Fact]

    public async Task AddMessage_ConversationNotFound()
    {
        var message = new Message("123", "jad", "bro got W rizz",1000, "1234");
        var profile = new Profile("jad", "Jad", "Haddad", "12345");
        var participants = new List<Profile> { profile };
        var conversation = new Conversation("1234", participants, 100000, profile.username);
        _conversationStoreMock.Setup(m => m.GetConversation(profile.username, message.conversationId)).ThrowsAsync(new ConversationNotFoundException());
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            await _conversationService.AddMessage(message);
        });
        _conversationStoreMock.Verify(mock => mock.GetConversation(profile.username, message.conversationId), Times.Once);
        _conversationStoreMock.Verify(mock => mock.ChangeConversationLastMessageTime(conversation, message.createdUnixTime), Times.Never);
        _messageStoreMock.Verify(mock => mock.AddMessage(message), Times.Never);
    }

    [Fact]

    public async Task AddMessage_MessageAlreadyExists()
    {
        var message = new Message("123", "jad", "bro got W rizz",1000, "1234");
        var profile1 = new Profile("jad", "Jad", "Haddad", "12345");
        var profile2 = new Profile("ronald", "ronald", "Haddad", "123456");
        var participants = new List<Profile> {profile1, profile2};
        var conversation = new Conversation("1234", participants, 100000, profile1.username);
        _conversationStoreMock.Setup(m => m.GetConversation(profile1.username, message.conversationId)).ReturnsAsync(conversation);
        _messageStoreMock.Setup(m => m.AddMessage(message)).ThrowsAsync(new MessageAlreadyExistsException());
        await Assert.ThrowsAsync<MessageAlreadyExistsException>(async () =>
        {
            await _conversationService.AddMessage(message);
        });
        _messageStoreMock.Verify(mock => mock.AddMessage(message), Times.Once);
    }



    [Fact]

    public async Task StartConversation()
    {
        var messageId = "1234";
        var senderUsername = "Ronald";
        var messageContent = "aha aha aha";
        long createdTime = 100000;
        var participants = new List<string> {"Ronald", "Jad"};
        var expectedId = "_Jad_Ronald";
        var actualId = await _conversationService.StartConversation(messageId, senderUsername, messageContent, createdTime, participants);
        Assert.Equal(expectedId, actualId);
    }

    [Fact]

    public async Task StartConversation_ConversationAlreadyExists()
    {
        var messageId = "1234";
        var senderUsername = "Ronald";
        var messageContent = "aha aha aha";
        long createdTime = 100000;
        var participants = new List<string> {"Jad", "Ronald"};
        var expectedId = "";
        foreach (var participant in participants)
        {
            expectedId += "_" + participant;
            var profile = new Profile(participant, "ok", "gym", "1234");
            _profileStoreMock.Setup(x => x.GetProfile(participant)).ReturnsAsync(profile);
        }
        _conversationStoreMock.Setup(x => x.CreateConversation(
            It.Is<Conversation>(c => 
                                        c.conversationId == expectedId 
                                     && c.lastMessageTime==createdTime 
                                     && c.username == senderUsername 
                                     && c.participants.All(p => typeof(Profile) == p.GetType())
                                     && c.participants.Any(p => p.username == "Ronald")
                                     && c.participants.Any(p => p.username == "Jad")
            )
        )).ThrowsAsync(new ConversationAlreadyExistsException());
        
        await Assert.ThrowsAsync<ConversationAlreadyExistsException>(async () =>
            await _conversationService.StartConversation(messageId, senderUsername, messageContent, createdTime, participants));
    }

    [Fact]

    public async Task StartConversation_MessageAlreadyExists()
    {
        var messageId = "1234";
        var senderUsername = "Ronald";
        var messageContent = "aha aha aha";
        long createdTime = 100000;
        var conversationId = "_Jad_Ronald";
        var participants = new List<string> {"Ronald", "Jad"};
        var message = new Message(messageId, senderUsername, messageContent, createdTime, conversationId);
        _messageStoreMock.Setup(x => x.AddMessage(message)).ThrowsAsync(new MessageAlreadyExistsException());
        await Assert.ThrowsAsync<MessageAlreadyExistsException>(async () =>
            await _conversationService.StartConversation(messageId, senderUsername, messageContent, createdTime, participants));

    }

    [Fact]

    public async Task StartConversation_SenderUsernameNotFound()
    {
        var senderProfile = new Profile("Jad", "Jad", "Haddad", "12345");
        var messageId = "123";
        var messageContent = "south park vs family guy";
        var createdTime = 10000;
        var participants = new List<string> {"Ronald", "Stewie"};
        await Assert.ThrowsAsync<ProfileNotFoundException>(async()=>
                await _conversationService.StartConversation(messageId, senderProfile.username, messageContent, createdTime, participants));
    }

}