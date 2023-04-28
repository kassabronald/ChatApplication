using ChatApplication.Configuration;
using ChatApplication.Exceptions;
using ChatApplication.Storage;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChatApplication.Web.IntegrationTests;

public class CosmosMessageStoreTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly IMessageStore _store;
    private static readonly string ConversationId = Guid.NewGuid().ToString();
    readonly Message _message1 = new Message(Guid.NewGuid().ToString(), "ronald", ConversationId, "hello", 1002);
    readonly Message _message2 = new Message(Guid.NewGuid().ToString(), "ronald", ConversationId, "hello", 1001);
    readonly Message _message3 = new Message(Guid.NewGuid().ToString(), "ronald", ConversationId, "hello", 1000);
    readonly ConversationMessage _conversationMessage1 = new ConversationMessage("ronald", "hello", 1002);
    readonly ConversationMessage _conversationMessage2 = new ConversationMessage("ronald", "hello", 1001);
    readonly ConversationMessage _conversationMessage3 = new ConversationMessage("ronald", "hello", 1000);
    private readonly List<ConversationMessage> _conversationMessageList;
    private readonly List<Message> _messageList;

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        foreach (var message in _messageList)
        {
            await _store.DeleteMessage(message);
        }
    }

    public CosmosMessageStoreTests(WebApplicationFactory<Program> factory)
    {
        _messageList = new List<Message>() { _message1, _message2, _message3 };
        _conversationMessageList = new List<ConversationMessage>()
            { _conversationMessage1, _conversationMessage2, _conversationMessage3 };
        
        var services = factory.Services;
        var cosmosSettings = services.GetRequiredService<IOptions<CosmosSettings>>().Value;
        var cosmosClient = new CosmosClient(cosmosSettings.ConnectionString);
        _store = new CosmosMessageStore(cosmosClient);
    }

    [Fact]
    public async Task AddMessage_Success()
    {
        await _store.AddMessage(_messageList[0]);
        var parameters = new GetMessagesParameters(_messageList[0].ConversationId, 100, "", 0);
        var actual = await _store.GetMessages(parameters);

        Assert.Equal(_conversationMessageList[0], actual.Messages[0]);
    }

    [Fact]
    public async Task AddMessage_MessageAlreadyExists()
    {
        await _store.AddMessage(_messageList[0]);
        await Assert.ThrowsAsync<MessageAlreadyExistsException>(async () =>
        {
            await _store.AddMessage(_messageList[0]);
        });
    }

    [Fact]
    public async Task DeleteMessage_Success()
    {
        await _store.AddMessage(_messageList[0]);
        await _store.DeleteMessage(_messageList[0]);
        var parameters = new GetMessagesParameters(_messageList[0].ConversationId, 100, "", 0);
        var actual = await _store.GetMessages(parameters);
        Assert.Empty(actual.Messages);
    }

    [Fact]
    public async Task DeleteMessage_EmptyMessage()
    {
        var message = new Message("", "", "", "", 1);
        await Assert.ThrowsAsync<CosmosException>(async () => { await _store.DeleteMessage(message); });
    }

    [Fact]
    public async Task GetConversationMessages_Success()
    {
        var expected = new List<ConversationMessage>();
        foreach (var message in _messageList)
        {
            await _store.AddMessage(message);
            expected.Add(new ConversationMessage(message.SenderUsername, message.Text,
                message.CreatedUnixTime));
        }
        var parameters = new GetMessagesParameters(_messageList[0].ConversationId, 100, "", 0);
        var actual = await _store.GetMessages(parameters);
        Assert.Equal(expected, actual.Messages);
    }


    [Fact]
    public async Task GetConversationMessages_WithContinuationToken()
    {
        foreach (var message in _messageList)
        {
            await _store.AddMessage(message);
        }
        var parametersFirstCall = new GetMessagesParameters(_messageList[0].ConversationId, 2, "", 0);
        var actual = await _store.GetMessages(parametersFirstCall);
        Assert.Equal(_conversationMessageList[0], actual.Messages[0]);
        Assert.Equal(_conversationMessageList[1], actual.Messages[1]);
        var parametersSecondCall = new GetMessagesParameters(_messageList[0].ConversationId, 2,
            actual.ContinuationToken, 0);
        var actual2 =
            await _store.GetMessages(parametersSecondCall);
        Assert.Equal(_conversationMessageList[2], actual2.Messages[0]);
    }


    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(150, 3)]
    [InlineData(2, 2)]
    [InlineData(null, 1)]
    public async Task GetConversationMessages_WithBadLimit(int limit, int actualCount)
    {
        foreach (var message in _messageList)
        {
            await _store.AddMessage(message);
        }
        var parameters = new GetMessagesParameters(_messageList[0].ConversationId, limit, "", 0);
        var actual = await _store.GetMessages(parameters);
        Assert.Equal(actualCount, actual.Messages.Count);
    }

    [Fact]
    public async Task GetConversationMessages_WithBadContinuationToken()
    {
        foreach (var message in _messageList)
        {
            await _store.AddMessage(message);
        }

        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            var parameters = new GetMessagesParameters(_messageList[0].ConversationId, 100, "bad token", 0);
            var actual = await _store.GetMessages(parameters);
        });
    }

    [Fact]
    public async Task GetConversationMessages_WithUnixTime()
    {
        foreach (var message in _messageList)
        {
            await _store.AddMessage(message);
        }
        
        var parameters = new GetMessagesParameters(_messageList[0].ConversationId, 100, "", 1000);
        var actual = await _store.GetMessages(parameters);
        Assert.Equal(_conversationMessageList[0], actual.Messages[0]);
        Assert.Equal(_conversationMessageList[1], actual.Messages[1]);
    }
}