using ChatApplication.Exceptions;
using ChatApplication.Storage;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApplication.Web.IntegrationTests;

public class CosmosConversationStoreTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime

{
    private readonly IConversationStore _store;
    private readonly string _conversationId = Guid.NewGuid().ToString();
    private readonly Profile _profile1 = new Profile("roro", "king", "97", "123");
    private readonly Profile _profile2 = new Profile("jad", "ok", "noob", "1234");
    private readonly Conversation _conversation;

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _store.DeleteConversation(_conversation);
    }
    
    public CosmosConversationStoreTests(WebApplicationFactory<Program> factory)
    {
        List<Profile> participants = new() { _profile1, _profile2 };
        _conversation = new Conversation(_conversationId, participants, 1000, _profile1.username);
        _store = factory.Services.GetRequiredService<IConversationStore>();
    }


    [Fact]

    public async Task GetConversation()
    {
        await _store.CreateConversation(_conversation);
        var conversation = await _store.GetConversation(_conversation.username, _conversationId);
        Assert.Equivalent(_conversation, conversation);
    }
    
    [Fact]
    
    public async Task GetConversation_NotFoundUsername()
    {
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            var randomId = Guid.NewGuid().ToString();
            await _store.GetConversation(randomId, _conversation.conversationId);
        });
    }
    
    [Fact]
    
    public async Task GetConversation_NotFoundConversationId()
    {
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            var randomId = Guid.NewGuid().ToString();
            await _store.GetConversation(_conversation.username, randomId);
        });
    }

    [Fact]

    public async Task GetConversation_EmptyUsername()
    {
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.GetConversation("", _conversation.conversationId);
        });
    }
    
    [Fact]
    
    public async Task GetConversation_EmptyConversationId()
    {
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.GetConversation(_conversation.username, "");
        });
    }
    
    [Fact]
    
    public async Task ChangeConversationLastMessageTime()
    {
        await _store.CreateConversation(_conversation);
        await _store.ChangeConversationLastMessageTime(_conversation, 1001);
        var conversation = await _store.GetConversation(_conversation.username,_conversation.conversationId);
        Assert.Equal(1001, conversation.lastMessageTime);
    }
    
    [Fact]
    
    public async Task ChangeConversationLastMessageTime_NotFound()
    {
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            await _store.ChangeConversationLastMessageTime(_conversation, 1001);
        });
    }
    
    [Fact]
    
    public async Task ChangeConversationLastMessageTime_EmptyConversation()
    {
        var conversation = new Conversation("", new List<Profile>(), 1000, "hey");
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.ChangeConversationLastMessageTime(conversation, 1001);
        });
    }

    [Fact]

    public async Task CreateConversation()
    {
        await _store.CreateConversation(_conversation);
        var conversation = await _store.GetConversation(_conversation.username, _conversation.conversationId);
        Assert.Equivalent(_conversation, conversation);
    }
    
    [Fact]
    //TODO: Change this to not throw exception
    
    public async Task CreateConversation_Conflict()
    {
        await _store.CreateConversation(_conversation);
        await Assert.ThrowsAsync<ConversationAlreadyExistsException>(async () =>
        {
            await _store.CreateConversation(_conversation);
        });
    }

    [Fact]

    public async Task CreateConversation_EmptyId()
    {
        var conversation = new Conversation("", new List<Profile>(), 1000, _conversation.conversationId);
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.CreateConversation(conversation);
        });
    }
    
    [Fact]
    
    public async Task CreateConversation_EmptyUsername()
    {
        var conversation = new Conversation(_conversation.conversationId, new List<Profile>(), 1000, "");
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.CreateConversation(conversation);
        });
    }

    [Fact]

    public async Task DeleteConversation()
    {
        await _store.CreateConversation(_conversation);
        await _store.DeleteConversation(_conversation);
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            await _store.GetConversation(_conversation.username, _conversation.conversationId);
        });
    }

    [Fact]

    public async Task DeleteConversation_EmptyId()
    {
        var conversation = new Conversation("", new List<Profile>(), 1000, _conversation.conversationId);
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.DeleteConversation(conversation);
        });
    }
    
    [Fact]
    
    public async Task DeleteConversation_EmptyUsername()
    {
        var conversation = new Conversation(_conversation.conversationId, new List<Profile>(), 1000, "");
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.DeleteConversation(conversation);
        });
    }
    
    
    
    
    
}