using ChatApplication.Exceptions;
using ChatApplication.Services;
using ChatApplication.Storage;
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
        var senderConversation = new Conversation("_jad_rizz", participants, 100000, senderProfile.Username);
        var receiverConversation = new Conversation("_jad_rizz", participants, 100000, receiverProfile.Username);
        _conversationStoreMock.Setup(m => m.GetConversation(senderProfile.Username, senderConversation.ConversationId)).ReturnsAsync(senderConversation);
        _conversationStoreMock.Setup(m => m.GetConversation(receiverProfile.Username, receiverConversation.ConversationId)).ReturnsAsync(receiverConversation);
        await _conversationService.AddMessage(message);
        _conversationStoreMock.Verify(mock => mock.GetConversation(senderProfile.Username, senderConversation.ConversationId), Times.Once);
        _conversationStoreMock.Verify(mock => mock.GetConversation(receiverProfile.Username, receiverConversation.ConversationId), Times.Once);
        _conversationStoreMock.Verify(mock => mock.ChangeConversationLastMessageTime(senderConversation, message.CreatedUnixTime), Times.Once);
        _conversationStoreMock.Verify(mock => mock.ChangeConversationLastMessageTime(receiverConversation, message.CreatedUnixTime), Times.Once);
        _messageStoreMock.Verify(mock => mock.AddMessage(message), Times.Once);
    }


    [Fact]

    public async Task AddMessage_ConversationNotFound()
    {
        var message = new Message("123", "jad", "bro got W rizz",1000, "1234");
        var profile = new Profile("jad", "Jad", "Haddad", "12345");
        var participants = new List<Profile> { profile };
        var conversation = new Conversation("1234", participants, 100000, profile.Username);
        _conversationStoreMock.Setup(m => m.GetConversation(profile.Username, message.ConversationId)).ThrowsAsync(new ConversationNotFoundException());
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            await _conversationService.AddMessage(message);
        });
        _conversationStoreMock.Verify(mock => mock.GetConversation(profile.Username, message.ConversationId), Times.Once);
        _conversationStoreMock.Verify(mock => mock.ChangeConversationLastMessageTime(conversation, message.CreatedUnixTime), Times.Never);
        _messageStoreMock.Verify(mock => mock.AddMessage(message), Times.Never);
    }

    [Fact]

    public async Task AddMessage_MessageAlreadyExists()
    {
        var message = new Message("123", "jad", "bro got W rizz",1000, "1234");
        var profile1 = new Profile("jad", "Jad", "Haddad", "12345");
        var profile2 = new Profile("ronald", "ronald", "Haddad", "123456");
        var participants = new List<Profile> {profile1, profile2};
        var conversation = new Conversation("1234", participants, 100000, profile1.Username);
        _conversationStoreMock.Setup(m => m.GetConversation(profile1.Username, message.ConversationId)).ReturnsAsync(conversation);
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
        _profileStoreMock.Setup(x => x.GetProfile("Jad")).ReturnsAsync(new Profile("Jad", "ok", "gym", "1234"));
        _profileStoreMock.Setup(x => x.GetProfile("Ronald")).ReturnsAsync(new Profile("Ronald", "ok", "gym", "1234"));
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
                                        c.ConversationId == expectedId 
                                     && c.LastMessageTime==createdTime 
                                     && c.Username == senderUsername 
                                     && c.Participants.All(p => typeof(Profile) == p.GetType())
                                        && c.Participants.Any(p => p.Username == "Jad")
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
                await _conversationService.StartConversation(messageId, senderProfile.Username, messageContent, createdTime, participants));
    }

    [Fact]

    public async Task GetConversationMessages()
    {
        var conversationId = "_karim_Ronald";
        var continuationToken = "someWeirdString";
        var conversationMessages = new List<ConversationMessage>();
        for (int i = 0; i < 2; i++)
        {
            var conversationMessage = new ConversationMessage("Ronald", "skot", 1);
            conversationMessages.Add(conversationMessage);
        }
        var nextContinuationToken = "anotherWeirdToken";
        var jsonContinuationTokenData =
            $@"[{{""token"":""{continuationToken}"",""range"":{{""min"":"""",""max"":""FF""}}}}]";
        var expectedReturnedJsonContinuationTokenData =  
            $@"[{{""token"":""{nextContinuationToken}"",""range"":{{""min"":"""",""max"":""FF""}}}}]";
        _messageStoreMock.Setup(x => x.GetConversationMessages(conversationId, 2, jsonContinuationTokenData, 0))
            .ReturnsAsync(
                new ConversationMessageAndToken(conversationMessages, expectedReturnedJsonContinuationTokenData));
        var expectedResult = new ConversationMessageAndToken(conversationMessages, nextContinuationToken);
        var actualResult = await _conversationService.GetConversationMessages(conversationId, 2, continuationToken, 0);
        Assert.Equal(expectedResult, actualResult);
    }
    
    [Fact]

    public async Task GetConversationMessages_NoContinuationToken()
    {
        var conversationId = "_karim_Ronald";
        var continuationToken = "";
        var conversationMessages = new List<ConversationMessage>();
        for (int i = 0; i < 2; i++)
        {
            var conversationMessage = new ConversationMessage("Ronald", "skot", 1);
            conversationMessages.Add(conversationMessage);
        }
        var nextContinuationToken = "anotherWeirdToken";
        var expectedReturnedJsonContinuationTokenData =  
            $@"[{{""token"":""{nextContinuationToken}"",""range"":{{""min"":"""",""max"":""FF""}}}}]";
        _messageStoreMock.Setup(x => x.GetConversationMessages(conversationId, 2, continuationToken, 0))
            .ReturnsAsync(
                new ConversationMessageAndToken(conversationMessages, expectedReturnedJsonContinuationTokenData));
        var expectedResult = new ConversationMessageAndToken(conversationMessages, nextContinuationToken);
        var actualResult = await _conversationService.GetConversationMessages(conversationId, 2, continuationToken, 0);
        Assert.Equal(expectedResult, actualResult);
    }
    
        
    [Fact]

    public async Task GetConversationMessages_NoContinuationTokenReturned()
    {
        var conversationId = "_karim_Ronald";
        var continuationToken = "";
        var conversationMessages = new List<ConversationMessage>();
        for (int i = 0; i < 2; i++)
        {
            var conversationMessage = new ConversationMessage("Ronald", "skot", 1);
            conversationMessages.Add(conversationMessage);
        }
        _messageStoreMock.Setup(x => x.GetConversationMessages(conversationId, 2, continuationToken, 0))
            .ReturnsAsync(
                new ConversationMessageAndToken(conversationMessages, null));
        var expectedResult = new ConversationMessageAndToken(conversationMessages, null);
        var actualResult = await _conversationService.GetConversationMessages(conversationId, 2, continuationToken, 0);
        Assert.Equal(expectedResult, actualResult);
    }

    [Fact]

    public async Task GetAllConversations()
    {
        var username = "jad";
        var continuationToken = "someWeirdString";
        var participants1 = new List<Profile>();
        var participants2 = new List<Profile>();
        participants1.Add(new Profile("jad", "mike", "o hearn", "1234"));
        participants1.Add(new Profile("karim", "karim", "haddad", "1234"));
        participants2.Add(new Profile("jad", "mike", "o hearn", "1234"));
        participants2.Add(new Profile("ronald", "ronald", "haddad", "1234"));
        var conversation1 = new Conversation("_jad_ronald", participants1, 1000, "jad");
        var conversation2 = new Conversation("_jad_karim", participants2, 1001, "jad");
        var conversations = new List<Conversation> { conversation1, conversation2 };
        var nextContinuationToken = "anotherWeirdToken";
        var jsonContinuationTokenData =
            $@"[{{""token"":""{continuationToken}"",""range"":{{""min"":"""",""max"":""FF""}}}}]";
        var expectedReturnedJsonContinuationTokenData =  
            $@"[{{""token"":""{nextContinuationToken}"",""range"":{{""min"":"""",""max"":""FF""}}}}]";
        _conversationStoreMock.Setup(x => x.GetAllConversations(username, 2, jsonContinuationTokenData, 0))
            .ReturnsAsync(
                new ConversationAndToken(conversations, expectedReturnedJsonContinuationTokenData));
        var expectedResult = new ConversationAndToken(conversations, nextContinuationToken);
        var actualResult = await _conversationService.GetAllConversations(username, 2, continuationToken, 0);
        Assert.Equivalent(expectedResult, actualResult);
    }
    
    [Fact]

    public async Task GetAllConversations_NoContinuationToken()
    {
        var username = "jad";
        var continuationToken = "";
        var participants1 = new List<Profile>();
        var participants2 = new List<Profile>();
        participants1.Add(new Profile("jad", "mike", "o hearn", "1234"));
        participants1.Add(new Profile("karim", "karim", "haddad", "1234"));
        participants2.Add(new Profile("jad", "mike", "o hearn", "1234"));
        participants2.Add(new Profile("ronald", "ronald", "haddad", "1234"));
        var conversation1 = new Conversation("_jad_ronald", participants1, 1000, "jad");
        var conversation2 = new Conversation("_jad_karim", participants2, 1001, "jad");
        var conversations = new List<Conversation> { conversation1, conversation2 };
        var nextContinuationToken = "anotherWeirdToken";
        var expectedReturnedJsonContinuationTokenData =  
            $@"[{{""token"":""{nextContinuationToken}"",""range"":{{""min"":"""",""max"":""FF""}}}}]";
        _conversationStoreMock.Setup(x => x.GetAllConversations(username, 2, continuationToken, 0))
            .ReturnsAsync(
                new ConversationAndToken(conversations, expectedReturnedJsonContinuationTokenData));
        var expectedResult = new ConversationAndToken(conversations, nextContinuationToken);
        var actualResult = await _conversationService.GetAllConversations(username, 2, continuationToken, 0);
        Assert.Equivalent(expectedResult, actualResult);
    }

    [Fact]

    public async Task GetAllConversations_NoContinuationTokenReturned()
    {
        var username = "jad";
        var continuationToken = "";
        var participants1 = new List<Profile>();
        var participants2 = new List<Profile>();
        participants1.Add(new Profile("jad", "mike", "o hearn", "1234"));
        participants1.Add(new Profile("karim", "karim", "haddad", "1234"));
        participants2.Add(new Profile("jad", "mike", "o hearn", "1234"));
        participants2.Add(new Profile("ronald", "ronald", "haddad", "1234"));
        var conversation1 = new Conversation("_jad_ronald", participants1, 1000, "jad");
        var conversation2 = new Conversation("_jad_karim", participants2, 1001, "jad");
        var conversations = new List<Conversation> { conversation1, conversation2 };
        _conversationStoreMock.Setup(x => x.GetAllConversations(username, 2, continuationToken, 0))
            .ReturnsAsync(
                new ConversationAndToken(conversations, null));
        var expectedResult = new ConversationAndToken(conversations, null);
        var actualResult = await _conversationService.GetAllConversations(username, 2, continuationToken, 0);
        Assert.Equivalent(expectedResult, actualResult);
    }
}