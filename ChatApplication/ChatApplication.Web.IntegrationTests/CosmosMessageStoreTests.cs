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
    Message _message1 = new Message(Guid.NewGuid().ToString(), "ronald", "hey bro wanna hit the gym", 1002, "456");
    Message _message2 = new Message(Guid.NewGuid().ToString(), "ronald", "hey bro wanna hit the gym", 1001, "456");
    Message _message3 = new Message(Guid.NewGuid().ToString(), "ronald", "hey bro wanna hit the gym", 1000, "456");
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
        var actual = await _store.GetConversationMessages(_messageList[0].conversationId);
        Assert.Equal(_messageList[0], actual[0]);
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
        var ok = await _store.GetConversationMessages(_messageList[0].conversationId);
        await _store.DeleteMessage(_messageList[0]);
        var actual = await _store.GetConversationMessages(_messageList[0].conversationId);
        Assert.Empty(actual);
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

    public async Task GetConversationMessages()
    {
        foreach (var message in _messageList)
        {
            await _store.DeleteMessage(message);
            await _store.AddMessage(message);
        }
        var actual = await _store.GetConversationMessages(_messageList[0].conversationId);
        Assert.Equal(_messageList, actual);
    }
}