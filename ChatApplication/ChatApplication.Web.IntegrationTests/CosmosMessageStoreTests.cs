using ChatApplication.Exceptions;
using ChatApplication.Storage;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApplication.Web.IntegrationTests;

public class CosmosMessageStoreTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    
    private readonly IMessageStore _store;
    private static string conversationId = Guid.NewGuid().ToString();
    Message _message1 = new Message(Guid.NewGuid().ToString(), "ronald", "hey bro wanna hit the gym", 1002, conversationId);
    Message _message2 = new Message(Guid.NewGuid().ToString(), "ronald", "hey bro wanna hit the gym", 1001, conversationId);
    Message _message3 = new Message(Guid.NewGuid().ToString(), "ronald", "hey bro wanna hit the gym", 1000, conversationId);
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
        _messageList = new List<Message>(){_message1, _message2, _message3};
        _store = factory.Services.GetRequiredService<IMessageStore>();
    }
    
    [Fact]
    public async Task AddMessage()
    {
        await _store.AddMessage(_messageList[0]);
        var actual = await _store.GetConversationMessagesUtil(_messageList[0].conversationId, 100, "", 0);
        Assert.Equal(_messageList[0], actual.messages[0]);
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
    
    public async Task DeleteMessage()
    {
        await _store.AddMessage(_messageList[0]);
        await _store.DeleteMessage(_messageList[0]);
        var actual = await _store.GetConversationMessagesUtil(_messageList[0].conversationId, 100, "", 0);
        Assert.Empty(actual.messages);
    }
    
    [Fact]

    public async Task DeleteEmptyMessage()
    {
        var message = new Message("", "", "",1, "");
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.DeleteMessage(message);
        });
    }

    [Fact]

    public async Task GetConversationMessagesUtil()
    {
        foreach (var message in _messageList)
        {
            await _store.AddMessage(message);
        }
        var actual = await _store.GetConversationMessagesUtil(_messageList[0].conversationId, 100, "", 0);
        Assert.Equal(_messageList, actual.messages);
    }
    //should we check for bad inputs?
    
    [Fact]

    public async Task GetConversationMessages()
    {
        var expected = new List<ConversationMessage>();
        foreach (var message in _messageList)
        {
            await _store.AddMessage(message);
            expected.Add(new ConversationMessage(message.senderUsername, message.messageContent, message.createdUnixTime));
        }
        var actual = await _store.GetConversationMessages(_messageList[0].conversationId, 100, "", 0);
        Assert.Equal(expected, actual.messages);
    }
    
    
    [Fact]
    public async Task GetConversationMessages_WithContinuationToken()
    {
        foreach (var message in _messageList)
        {
            await _store.AddMessage(message);
        }
        var actual = await _store.GetConversationMessagesUtil(_messageList[0].conversationId, 2, "", 0);
        Assert.Equal(_messageList[0], actual.messages[0]);
        Assert.Equal(_messageList[1], actual.messages[1]);
        var actual2 = await _store.GetConversationMessagesUtil(_messageList[0].conversationId, 2, actual.continuationToken, 0);
        Assert.Equal(_messageList[2], actual2.messages[0]);
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
        var actual = await _store.GetConversationMessagesUtil(_messageList[0].conversationId, limit, "", 0);
        Assert.Equal(actualCount, actual.messages.Count);
    }
    
    [Fact]
    public async Task GetConversationMessages_WithBadContinuationToken()
    {
        foreach (var message in _messageList)
        {
            await _store.AddMessage(message);
        }
        Assert.ThrowsAsync<CosmosException>( async () =>
        {
            var actual = await _store.GetConversationMessagesUtil(_messageList[0].conversationId, 100, "bad token", 0);
        });
    }
    
    [Fact]
    public async Task GetConversationMessages_WithUnixTime()
    {
        foreach (var message in _messageList)
        {
            await _store.AddMessage(message);
        }
        var actual = await _store.GetConversationMessagesUtil(_messageList[0].conversationId, 100, "", 1000);
        Assert.Equal(_messageList[0], actual.messages[0]);
        Assert.Equal(_messageList[1], actual.messages[1]);
    }
}