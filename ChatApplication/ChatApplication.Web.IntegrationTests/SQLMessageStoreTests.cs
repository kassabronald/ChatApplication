using ChatApplication.Configuration;
using ChatApplication.Exceptions;
using ChatApplication.Storage;
using ChatApplication.Storage.SQL;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChatApplication.Web.IntegrationTests;

public class SQLMessageStoreTests: IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly IMessageStore _messageStore;
    private readonly IConversationStore _conversationStore;
    private readonly IProfileStore _profileStore;
    private readonly string _conversationId;
    private readonly string _profile1_username;
    private readonly string _profile2_username;
    private readonly Profile _profile1;
    private readonly Profile _profile2;
    private readonly UserConversation _userConversation1;
    private readonly UserConversation _userConversation2;
    private readonly Message _message1;
    private readonly Message _message2;
    private readonly Message _message3;
    private readonly ConversationMessage _conversationMessage1;
    private readonly ConversationMessage _conversationMessage2;
    private readonly ConversationMessage _conversationMessage3;
    private readonly List<ConversationMessage> _conversationMessageList;
    private readonly List<Message> _messageList;

    public async Task InitializeAsync()
    {
        await _profileStore.AddProfile(_profile1);
        await _profileStore.AddProfile(_profile2);
        await _conversationStore.CreateUserConversation(_userConversation1);
        await _conversationStore.CreateUserConversation(_userConversation2);
    }

    public async Task DisposeAsync()
    {
        await _profileStore.DeleteProfile(_profile1.Username);
        await _profileStore.DeleteProfile(_profile2.Username);
    }


    public SQLMessageStoreTests(WebApplicationFactory<Program> factory)
    {
        
        _conversationId = Guid.NewGuid().ToString();
        _profile1_username = Guid.NewGuid().ToString();
        _profile2_username = Guid.NewGuid().ToString();
        
        _message1 = new Message(Guid.NewGuid().ToString(), _profile1_username, _conversationId, "hello", 1002);
        _message2 = new Message(Guid.NewGuid().ToString(), _profile1_username, _conversationId, "hello", 1001);
        _message3 = new Message(Guid.NewGuid().ToString(), _profile1_username, _conversationId, "hello", 1000);

        _profile1 = new Profile(_profile1_username, "ronald", "ronald", "ronald");
        _profile2 = new Profile(_profile2_username, "jad", "jad", "jad");
        
        _conversationMessage1 = new ConversationMessage(_profile1_username, "hello", 1002);
        _conversationMessage2 = new ConversationMessage(_profile1_username, "hello", 1001);
        _conversationMessage3 = new ConversationMessage(_profile1_username, "hello", 1000);

        _messageList = new List<Message>() { _message1, _message2, _message3 };
        _conversationMessageList = new List<ConversationMessage>() { _conversationMessage1, _conversationMessage2, _conversationMessage3 };
        
        _userConversation1 = new UserConversation(_conversationId, new List<Profile>() { _profile2 }, 0, _profile1.Username);
        _userConversation2 = new UserConversation(_conversationId, new List<Profile>() { _profile1 }, 0, _profile2.Username);
        
        var services = factory.Services;
        var sqlSettings = services.GetRequiredService<IOptions<SQLSettings>>();
        _conversationStore = new SQLConversationStore(sqlSettings);
        _profileStore = new SQLProfileStore(sqlSettings);
        _messageStore = new SQLMessageStore(sqlSettings);
    }
    
    [Fact]
    public async Task AddMessage_Success()
    {
        
        await _conversationStore.CreateUserConversation(new UserConversation(_conversationId, new List<Profile>() { _profile2 }, 0, _profile1.Username));
        await _messageStore.AddMessage(_messageList[0]);
        
        var parameters = new GetMessagesParameters(_messageList[0].ConversationId, 100, "", 0);
        var actual = await _messageStore.GetMessages(parameters);

        Assert.Equal(_conversationMessageList[0], actual.Messages[0]);
    }

    [Fact]
    public async Task AddMessage_MessageAlreadyExists()
    {
        await _messageStore.AddMessage(_messageList[0]);
        
        await Assert.ThrowsAsync<MessageAlreadyExistsException>(async () =>
        {
            await _messageStore.AddMessage(_messageList[0]);
        });
    }

    [Fact]
    public async Task DeleteMessage_Success()
    {
        await _messageStore.AddMessage(_messageList[0]);
        await _messageStore.DeleteMessage(_messageList[0]);
        
        var parameters = new GetMessagesParameters(_messageList[0].ConversationId, 100, "", 0);
        var actual = await _messageStore.GetMessages(parameters);
        
        Assert.Empty(actual.Messages);
    }
    

    [Fact]
    public async Task GetConversationMessages_Success()
    {
        var expected = new List<ConversationMessage>();
        foreach (var message in _messageList)
        {
            await _messageStore.AddMessage(message);
            expected.Add(new ConversationMessage(message.SenderUsername, message.Text,
                message.CreatedUnixTime));
        }
        
        var parameters = new GetMessagesParameters(_messageList[0].ConversationId, 100, "", 0);
        var actual = await _messageStore.GetMessages(parameters);
        
        Assert.Equal(expected, actual.Messages);
    }


    [Fact]
    public async Task GetConversationMessages_WithContinuationToken()
    {
        foreach (var message in _messageList)
        {
            await _messageStore.AddMessage(message);
        }
        var parametersFirstCall = new GetMessagesParameters(_messageList[0].ConversationId, 2, "", 0);
        var actual = await _messageStore.GetMessages(parametersFirstCall);
        
        Assert.Equal(_conversationMessageList[0], actual.Messages[0]);
        Assert.Equal(_conversationMessageList[1], actual.Messages[1]);
        
        var parametersSecondCall = new GetMessagesParameters(_messageList[0].ConversationId, 2, actual.ContinuationToken, 0);
        var actual2 = await _messageStore.GetMessages(parametersSecondCall);
        
        Assert.Equal(_conversationMessageList[2], actual2.Messages[0]);
    }


    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(150, 3)]
    [InlineData(null, 1)]
    public async Task GetConversationMessages_WithBadLimit(int limit, int actualCount)
    {
        foreach (var message in _messageList)
        {
            await _messageStore.AddMessage(message);
        }
        
        var parameters = new GetMessagesParameters(_messageList[0].ConversationId, limit, "", 0);
        var actual = await _messageStore.GetMessages(parameters);
        
        Assert.Equal(actualCount, actual.Messages.Count);
    }

    [Fact]
    public async Task GetConversationMessages_WithBadContinuationToken()
    {
        foreach (var message in _messageList)
        {
            await _messageStore.AddMessage(message);
        }

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            var parameters = new GetMessagesParameters(_messageList[0].ConversationId, 100, "bad token", 0);
            var actual = await _messageStore.GetMessages(parameters);
        });
    }

    [Fact]
    public async Task GetConversationMessages_WithUnixTime()
    {
        foreach (var message in _messageList)
        {
            await _messageStore.AddMessage(message);
        }
        
        var parameters = new GetMessagesParameters(_messageList[0].ConversationId, 100, "", 1000);
        var actual = await _messageStore.GetMessages(parameters);
        
        Assert.Equal(_conversationMessageList[0], actual.Messages[0]);
        Assert.Equal(_conversationMessageList[1], actual.Messages[1]);
    }
}