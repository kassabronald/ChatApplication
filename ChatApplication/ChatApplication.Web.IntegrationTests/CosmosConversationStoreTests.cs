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
        _conversation = new Conversation(_conversationId, participants, 1000);
        _store = factory.Services.GetRequiredService<IConversationStore>();
    }


    [Fact]

    public async Task GetConversation()
    {
        await _store.StartConversation(_conversation);
        var conversation = await _store.GetConversation(_conversationId);
        Assert.Equivalent(_conversation, conversation);
    }
    
    [Fact]
    
    public async Task GetConversation_NotFound()
    {
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            await _store.GetConversation("haha thats an id that doesnt exist LOL");
        });
    }

    [Fact]

    public async Task GetConversation_EmptyId()
    {
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.GetConversation("");
        });
    }
    
    [Fact]
    
    public async Task ChangeConversationLastMessageTime()
    {
        await _store.StartConversation(_conversation);
        await _store.ChangeConversationLastMessageTime(_conversation, 1001);
        var conversation = await _store.GetConversation(_conversationId);
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
        var conversation = new Conversation("", new List<Profile>(), 1000);
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.ChangeConversationLastMessageTime(conversation, 1001);
        });
    }

    [Fact]

    public async Task StartConversation()
    {
        await _store.StartConversation(_conversation);
        var conversation = await _store.GetConversation(_conversationId);
        Assert.Equivalent(_conversation, conversation);
    }
    
    [Fact]
    
    public async Task StartConversation_Conflict()
    {
        await _store.StartConversation(_conversation);
        await Assert.ThrowsAsync<ConversationAlreadyExistsException>(async () =>
        {
            await _store.StartConversation(_conversation);
        });
    }

    [Fact]

    public async Task StartConversation_EmptyId()
    {
        var conversation = new Conversation("", new List<Profile>(), 1000);
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.StartConversation(conversation);
        });
    }

    [Fact]

    public async Task DeleteConversation()
    {
        await _store.StartConversation(_conversation);
        await _store.DeleteConversation(_conversation);
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            await _store.GetConversation(_conversationId);
        });
    }

    [Fact]

    public async Task DeleteConversation_EmptyId()
    {
        var conversation = new Conversation("", new List<Profile>(), 1000);
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.DeleteConversation(conversation);
        });
    }
    
    
    
    
    
}