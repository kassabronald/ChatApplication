using ChatApplication.Exceptions;
using ChatApplication.Exceptions.ConversationParticipantsExceptions;
using ChatApplication.ServiceBus.Interfaces;
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
    private readonly Mock<IAddMessageServiceBusPublisher> _addMessageServiceBusPublisherMock = new();
    private readonly Mock<IStartConversationServiceBusPublisher> _startConversationServiceBusPublisherMock = new();
    private readonly ConversationService _conversationService;
    public ConversationServiceTests()
    {
        _conversationService = new ConversationService(_messageStoreMock.Object, _conversationStoreMock.Object, 
            _profileStoreMock.Object, _addMessageServiceBusPublisherMock.Object, _startConversationServiceBusPublisherMock.Object);
    }

    [Fact]

    public async Task AddMessage_Success()
    {
        var message = new Message("123", "jad", "bro got W rizz", 1000,"_jad_rizz");
        var participantsUsernames = new List<string> { "jad", "rizz" };
        var recipientProfile = new Profile("rizz", "ok", "ok", "ok");
        var senderConversation = new UserConversation("_jad_rizz", new List<Profile>{recipientProfile}, 1000, "jad");
        _conversationStoreMock.Setup(m => m.GetUserConversation("jad", "_jad_rizz")).ReturnsAsync(senderConversation);
        await _conversationService.AddMessage(message);
        _conversationStoreMock.Verify(mock => mock.UpdateConversationLastMessageTime(senderConversation, message.CreatedUnixTime), Times.Once);
        _messageStoreMock.Verify(mock => mock.AddMessage(message), Times.Once);
    }


    [Fact]

    public async Task AddMessage_ConversationNotFound()
    {
        var message = new Message("123", "jad", "bro got W rizz", 1000,"_jad_rizz");
        _conversationStoreMock.Setup(m => m.GetUserConversation("jad", "_jad_rizz")).ThrowsAsync(new ConversationNotFoundException("Conversation not found"));
        await Assert.ThrowsAsync<ConversationNotFoundException> (async()  =>
        {
            await _conversationService.AddMessage(message);
        });
        _messageStoreMock.Verify(mock => mock.AddMessage(message), Times.Never);
    }

    [Fact]

    public async Task AddMessage_MessageAlreadyExists()
    {
        var message = new Message("123", "jad", "bro got W rizz",1000, "1234");
        var recipientProfile = new Profile("ronald", "ronald", "Haddad", "123456");
        var recipients = new List<Profile> {recipientProfile};
        var conversation = new UserConversation("1234", recipients, 100000,"jad");
        _conversationStoreMock.Setup(m => m.GetUserConversation("jad",message.ConversationId)).ReturnsAsync(conversation);
        _messageStoreMock.Setup(m => m.AddMessage(message)).ThrowsAsync(new MessageAlreadyExistsException("Message already exists"));
        var exception = await Record.ExceptionAsync(async () =>
            await _conversationService.AddMessage(message));
        Assert.Null(exception);
        
        _messageStoreMock.Verify(mock => mock.AddMessage(message), Times.Once);
    }



    [Fact]

    public async Task StartConversation_Success()
    {
        var messageId = "1234";
        var senderUsername = "Ronald";
        var messageContent = "aha aha aha";
        long createdTime = 100000;
        var participants = new List<string> {"Ronald", "Jad"};
        var expectedId = "_Jad_Ronald";
        _profileStoreMock.Setup(x => x.GetProfile("Jad")).ReturnsAsync(new Profile("Jad", "ok", "gym", "1234"));
        _profileStoreMock.Setup(x => x.GetProfile("Ronald")).ReturnsAsync(new Profile("Ronald", "ok", "gym", "1234"));
        var startConversationParameters = new StartConversationParameters(messageId, senderUsername, messageContent, createdTime, participants);
        var actualId = await _conversationService.StartConversation(startConversationParameters);
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
        _conversationStoreMock.Setup(x => x.CreateUserConversation(
            It.Is<UserConversation>(c => 
                                        c.ConversationId == expectedId 
                                     && c.LastMessageTime==createdTime 
                                     && c.Username == senderUsername 
                                     && c.Recipients.All(p => typeof(Profile) == p.GetType())
                                        && c.Recipients.Any(p => p.Username == "Jad")
            )
        )).ThrowsAsync(new ConversationAlreadyExistsException("Conversation already exists"));
        var startConversationParameters = new StartConversationParameters(messageId, senderUsername, messageContent, createdTime, participants);
        var exception = await Record.ExceptionAsync(async () =>
            await _conversationService.StartConversation(startConversationParameters));
        Assert.Null(exception);
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
        foreach(var participant in participants)
        {
            var profile = new Profile(participant, "ok", "gym", "1234");
            _profileStoreMock.Setup(x => x.GetProfile(participant)).ReturnsAsync(profile);
        }
        var message = new Message(messageId, senderUsername, messageContent, createdTime, conversationId);
        _messageStoreMock.Setup(x => x.AddMessage(message)).ThrowsAsync(new MessageAlreadyExistsException("Message already exists"));
        var startConversationParameters = new StartConversationParameters(messageId, senderUsername, messageContent, createdTime, participants);
        await Assert.ThrowsAsync<MessageAlreadyExistsException>(async () =>
            await _conversationService.StartConversation(startConversationParameters));

    }

    [Fact]

    public async Task StartConversation_SenderUsernameNotFound()
    {
        var senderProfile = new Profile("Jad", "Jad", "Haddad", "12345");
        var messageId = "123";
        var messageContent = "south park vs family guy";
        var createdTime = 10000;
        var participants = new List<string> {"Ronald", "Stewie"};
        _profileStoreMock.Setup(x => x.GetProfile("Ronald")).ReturnsAsync(senderProfile);
        _profileStoreMock.Setup(x => x.GetProfile("Stewie")).ReturnsAsync(new Profile("Stewie", "Stewie", "Griffin", "12345"));
        var startConversationParameters = new StartConversationParameters(messageId, senderProfile.Username, messageContent, createdTime, participants);
        var exception = await Record.ExceptionAsync(async()=>
                await _conversationService.StartConversation(startConversationParameters));
        Assert.Null(exception);
    }

    [Fact]

    public async Task StartConversation_DuplicateParticipant()
    {
        var senderProfile = new Profile("Ronald", "Jad", "Haddad", "12345");
        var messageId = "123";
        var messageContent = "south park vs family guy";
        var createdTime = 10000;
        var participants = new List<string> {"Ronald", "Stewie"};
        _profileStoreMock.Setup(x => x.GetProfile("Ronald")).ReturnsAsync(senderProfile);
        _profileStoreMock.Setup(x => x.GetProfile("Stewie")).ReturnsAsync(new Profile("Stewie", "Stewie", "Griffin", "12345"));
        var startConversationParameters = new StartConversationParameters(messageId, senderProfile.Username, messageContent, createdTime, participants);
        var exception = await Record.ExceptionAsync(async () =>
            await _conversationService.StartConversation(startConversationParameters));
        Assert.Null(exception);
    }

    [Fact]

    public async Task GetConversationMessages_Success()
    {
        var conversationId = "_karim_Ronald";
        var continuationToken = "someWeirdString";
        var parameters = new GetMessagesParameters(conversationId, 2, continuationToken, 0);
        var conversationMessages = new List<ConversationMessage>();
        for (int i = 0; i < 2; i++)
        {
            var conversationMessage = new ConversationMessage("Ronald", "skot", 1);
            conversationMessages.Add(conversationMessage);
        }
        var nextContinuationToken = "anotherWeirdToken";
        _messageStoreMock.Setup(x => x.GetMessages(parameters))
            .ReturnsAsync(
                new GetMessagesResult(conversationMessages, nextContinuationToken));
        var expectedResult = new GetMessagesResult(conversationMessages, nextContinuationToken);
        var actualResult = await _conversationService.GetMessages(parameters);
        Assert.Equal(expectedResult, actualResult);
    }
    
    [Fact]

    public async Task GetConversationMessages_NoContinuationToken()
    {
        var conversationId = "_karim_Ronald";
        var continuationToken = "";
        var parameters = new GetMessagesParameters(conversationId, 2, continuationToken, 0);
        var conversationMessages = new List<ConversationMessage>();
        for (int i = 0; i < 2; i++)
        {
            var conversationMessage = new ConversationMessage("Ronald", "skot", 1);
            conversationMessages.Add(conversationMessage);
        }
        var nextContinuationToken = "anotherWeirdToken";
        _messageStoreMock.Setup(x => x.GetMessages(parameters))
            .ReturnsAsync(
                new GetMessagesResult(conversationMessages, nextContinuationToken));
        var expectedResult = new GetMessagesResult(conversationMessages, nextContinuationToken);
        var actualResult = await _conversationService.GetMessages(parameters);
        Assert.Equal(expectedResult, actualResult);
    }
    
        
    [Fact]

    public async Task GetConversationMessages_NoContinuationTokenReturned()
    {
        var conversationId = "_karim_Ronald";
        var continuationToken = "";
        var parameters = new GetMessagesParameters(conversationId, 2, continuationToken, 0);
        var conversationMessages = new List<ConversationMessage>();
        for (int i = 0; i < 2; i++)
        {
            var conversationMessage = new ConversationMessage("Ronald", "skot", 1);
            conversationMessages.Add(conversationMessage);
        }
        _messageStoreMock.Setup(x => x.GetMessages(parameters))
            .ReturnsAsync(
                new GetMessagesResult(conversationMessages, null));
        var expectedResult = new GetMessagesResult(conversationMessages, null);
        var actualResult = await _conversationService.GetMessages(parameters);
        Assert.Equal(expectedResult, actualResult);
    }

    [Fact]

    public async Task GetAllConversations_Success()
    {
        var username = "jad";
        var continuationToken = "someWeirdString";
        var participants1 = new List<Profile>();
        var participants2 = new List<Profile>();
        participants1.Add(new Profile("jad", "mike", "o hearn", "1234"));
        participants1.Add(new Profile("karim", "karim", "haddad", "1234"));
        participants2.Add(new Profile("jad", "mike", "o hearn", "1234"));
        participants2.Add(new Profile("ronald", "ronald", "haddad", "1234"));
        var conversation1 = new UserConversation("_jad_ronald", participants1, 1000, "jad");
        var conversation2 = new UserConversation("_jad_karim", participants2, 1001, "jad");
        var conversations = new List<UserConversation> { conversation1, conversation2 };
        var nextContinuationToken = "anotherWeirdToken";
        var parameters = new GetConversationsParameters(username, 2, continuationToken, 0);

        _conversationStoreMock.Setup(x => x.GetConversations(parameters))
            .ReturnsAsync(
                new GetConversationsResult(conversations, nextContinuationToken));
        var expectedResult = new GetConversationsResult(conversations, nextContinuationToken);
        var actualResult = await _conversationService.GetConversations(parameters);
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
        var conversation1 = new UserConversation("_jad_ronald", participants1, 1000, "jad");
        var conversation2 = new UserConversation("_jad_karim", participants2, 1001, "jad");
        var conversations = new List<UserConversation> { conversation1, conversation2 };
        var nextContinuationToken = "anotherWeirdToken";
        var parameters = new GetConversationsParameters(username, 2, continuationToken, 0);
        _conversationStoreMock.Setup(x => x.GetConversations(parameters))
            .ReturnsAsync(
                new GetConversationsResult(conversations, nextContinuationToken));
        var expectedResult = new GetConversationsResult(conversations, nextContinuationToken);
        var actualResult = await _conversationService.GetConversations(parameters);
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
        var conversation1 = new UserConversation("_jad_ronald", participants1, 1000, "jad");
        var conversation2 = new UserConversation("_jad_karim", participants2, 1001, "jad");
        var conversations = new List<UserConversation> { conversation1, conversation2 };
        var parameters = new GetConversationsParameters(username, 2, continuationToken, 0);
        _conversationStoreMock.Setup(x => x.GetConversations(parameters))
            .ReturnsAsync(
                new GetConversationsResult(conversations, null));
        var expectedResult = new GetConversationsResult(conversations, null);
        var actualResult = await _conversationService.GetConversations(parameters);
        Assert.Equivalent(expectedResult, actualResult);
    }
    
    [Fact]
    public async Task EnqueueMessage_Success()
    {
        var conversationId = "_jad_karim";
        var message = new Message("123", "jad", "hello", 1000, conversationId);
        _messageStoreMock.Setup(x => x.GetMessage(message.ConversationId, message.MessageId))
            .ThrowsAsync(new MessageNotFoundException("message not found"));
        var exception = await Record.ExceptionAsync(
            async () => await _conversationService.EnqueueAddMessage(message));
        Assert.Null(exception);
        _addMessageServiceBusPublisherMock.Verify(x => x.Send(message), Times.Once);
    }
    
    [Fact]
    public async Task EnqueueMessage_MessageAlreadyExists()
    {
        var conversationId = "_jad_karim";
        var message = new Message("123", "jad", "hello", 1000, conversationId);
        _messageStoreMock.Setup(x => x.GetMessage(message.ConversationId, message.MessageId))
            .ReturnsAsync(message);
        await Assert.ThrowsAsync<MessageAlreadyExistsException> (async () =>
        {
            await _conversationService.EnqueueAddMessage(message);
        });
        _addMessageServiceBusPublisherMock.Verify(x => x.Send(message), Times.Never);
    }
    
    [Fact]
    public async Task EnqueueStartConversation_Success()
    {
        var conversationId = "_jad_karim";
        var participantsList = new List<string> { "jad", "karim" };
        var message = new Message("123", "jad", "hello", 1000, conversationId);
        _messageStoreMock.Setup(x => x.GetMessage(message.ConversationId, message.MessageId))
            .ThrowsAsync(new MessageNotFoundException("message not found"));
        var startConversationParameters = new StartConversationParameters(message.MessageId, "jad", "hello", 1000, participantsList);
        var exception = await Record.ExceptionAsync(
            async () => await _conversationService.EnqueueStartConversation(startConversationParameters));
        Assert.Null(exception);
        _startConversationServiceBusPublisherMock.Verify(x => x.Send(startConversationParameters), Times.Once);
    }
    
    [Fact]
    public async Task EnqueueStartConversation_MessageAlreadyExists()
    {
        var conversationId = "_jad_karim";
        var participantsList = new List<string> { "jad", "karim" };
        var message = new Message("123", "jad", "hello", 1000, conversationId);
        _messageStoreMock.Setup(x => x.GetMessage(message.ConversationId, message.MessageId))
            .ReturnsAsync(message);
        var startConversationParameters = new StartConversationParameters(message.MessageId, "jad", "hello", 1000, participantsList);
        await Assert.ThrowsAsync<MessageAlreadyExistsException> (async () =>
        {
            await _conversationService.EnqueueStartConversation(startConversationParameters);
        });
        _startConversationServiceBusPublisherMock.Verify(x => x.Send(startConversationParameters), Times.Never);
    }

    [Fact]
    public async Task EnqueueStartConversation_ReceiverNotFound()
    {
        var conversationId = "_jad_karim";
        var participantsList = new List<string> { "jad", "karim" };
        var message = new Message("123", "jad", "hello", 1000, conversationId);
        var startConversationParameters = new StartConversationParameters(message.MessageId, "jad", "hello", 1000, participantsList);
        _profileStoreMock.Setup(x => x.GetProfile("jad"))
            .ReturnsAsync(new Profile("jad", "mike", "o hearn", "1234"));
        _profileStoreMock.Setup(x => x.GetProfile("karim"))
            .ThrowsAsync(new ProfileNotFoundException("profile not found"));
        await Assert.ThrowsAsync<ProfileNotFoundException> (async () =>
        {
            await _conversationService.EnqueueStartConversation(startConversationParameters);
        });
        _startConversationServiceBusPublisherMock.Verify(x => x.Send(startConversationParameters), Times.Never);
    }

    [Fact]
    public async Task EnqueueStartConversation_SenderNotFound()
    {
        var conversationId = "_jad_karim";
        var participantsList = new List<string> { "toufic", "karim" };
        var message = new Message("123", "jad", "hello", 1000, conversationId);
        var startConversationParameters = new StartConversationParameters(message.MessageId, "jad", "hello", 1000, participantsList);
        await Assert.ThrowsAsync<SenderNotFoundException> (async () =>
        {
            await _conversationService.EnqueueStartConversation(startConversationParameters);
        });
        _startConversationServiceBusPublisherMock.Verify(x => x.Send(startConversationParameters), Times.Never);
    }
    
    [Fact]
    public async Task EnqueueStartConversation_DuplicateParticipants()
    {
        var conversationId = "_jad_karim";
        var participantsList = new List<string> { "jad", "jad" };
        var message = new Message("123", "jad", "hello", 1000, conversationId);
        var startConversationParameters = new StartConversationParameters(message.MessageId, "jad", "hello", 1000, participantsList);
        await Assert.ThrowsAsync<DuplicateParticipantException> (async () =>
        {
            await _conversationService.EnqueueStartConversation(startConversationParameters);
        });
        _startConversationServiceBusPublisherMock.Verify(x => x.Send(startConversationParameters), Times.Never);
    }
    
    
}