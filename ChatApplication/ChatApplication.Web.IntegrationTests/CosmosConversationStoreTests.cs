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
    private readonly Profile _profile1 = new Profile(Guid.NewGuid().ToString(), "king", "97", "123");
    private readonly Profile _profile2 = new Profile(Guid.NewGuid().ToString(), "ok", "noob", "1234");
    private readonly Profile _profile3 = new Profile(Guid.NewGuid().ToString(), "k", "rim", "12345");
    private readonly Profile _profile4 = new Profile(Guid.NewGuid().ToString(), "k", "rim", "123456");
    private readonly UserConversation _conversation1;
    private readonly UserConversation _conversation2;
    private readonly UserConversation _conversation3;
    private readonly List<UserConversation> _conversationList;

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        foreach (var conversation in _conversationList)
        {
            await _store.DeleteConversation(conversation);
        }
    }
    
    public CosmosConversationStoreTests(WebApplicationFactory<Program> factory)
    {
        List<Profile> participants1 = new() { _profile1, _profile2 };
        List<Profile> participants2 = new() { _profile1, _profile3 };
        List<Profile> participants3 = new() { _profile1, _profile4 };
        _conversation1 = new UserConversation(Guid.NewGuid().ToString(), participants1, 1002, _profile1.Username);
        _conversation2 = new UserConversation(Guid.NewGuid().ToString(), participants2, 1001, _profile1.Username);
        _conversation3 = new UserConversation(Guid.NewGuid().ToString(), participants3, 1000, _profile1.Username);
        _conversationList = new List<UserConversation>(){_conversation1, _conversation2, _conversation3};
        _store = factory.Services.GetRequiredService<IConversationStore>();
    }


    [Fact]

    public async Task GetConversation()
    {
        await _store.CreateConversation(_conversationList[0]);
        var conversation = await _store.GetConversation(_conversationList[0].Username, _conversationList[0].ConversationId);
        Assert.Equivalent(_conversationList[0], conversation);
    }
    
    [Fact]
    
    public async Task GetConversation_NotFoundUsername()
    {
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            var randomId = Guid.NewGuid().ToString();
            await _store.GetConversation(randomId, _conversationList[0].ConversationId);
        });
    }
    
    [Fact]
    
    public async Task GetConversation_NotFoundConversationId()
    {
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            var randomId = Guid.NewGuid().ToString();
            await _store.GetConversation(_conversationList[0].Username, randomId);
        });
    }

    [Fact]
    
    public async Task GetConversation_EmptyConversationId()
    {
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.GetConversation(_conversationList[0].Username, "");
        });
    }
    
    [Fact]
    
    public async Task ChangeConversationLastMessageTime()
    {
        await _store.CreateConversation(_conversationList[0]);
        await _store.UpdateConversationLastMessageTime(_conversationList[0], 1001);
        var conversation = await _store.GetConversation(_conversationList[0].Username,_conversationList[0].ConversationId);
        Assert.Equal(1001, conversation.LastMessageTime);
    }
    
    [Fact]
    
    public async Task ChangeConversationLastMessageTime_NotFound()
    {
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            await _store.UpdateConversationLastMessageTime(_conversationList[0], 1001);
        });
    }
    
    [Fact]
    
    public async Task ChangeConversationLastMessageTime_EmptyConversation()
    {
        var conversation = new UserConversation("", new List<Profile>(), 1000, "hey");
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.UpdateConversationLastMessageTime(conversation, 1001);
        });
    }

    [Fact]

    public async Task CreateConversation()
    {
        await _store.CreateConversation(_conversationList[0]);
        var conversation = await _store.GetConversation(_conversationList[0].Username, _conversationList[0].ConversationId);
        Assert.Equivalent(_conversationList[0], conversation);
    }
    
    [Fact]
    //TODO: Change this to not throw exception
    
    public async Task CreateConversation_Conflict()
    {
        await _store.CreateConversation(_conversationList[0]);
        await Assert.ThrowsAsync<ConversationAlreadyExistsException>(async () =>
        {
            await _store.CreateConversation(_conversationList[0]);
        });
    }

    [Fact]

    public async Task CreateConversation_EmptyId()
    {
        var conversation = new UserConversation("", new List<Profile>(), 1000, _conversationList[0].ConversationId);
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.CreateConversation(conversation);
        });
    }


    [Fact]

    public async Task DeleteConversation()
    {
        await _store.CreateConversation(_conversationList[0]);
        await _store.DeleteConversation(_conversationList[0]);
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            await _store.GetConversation(_conversationList[0].Username, _conversationList[0].ConversationId);
        });
    }

    [Fact]

    public async Task DeleteConversation_EmptyId()
    {
        var conversation = new UserConversation("", new List<Profile>(), 1000, _conversationList[0].ConversationId);
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.DeleteConversation(conversation);
        });
    }
    
    [Fact]

    public async Task GetAllConversation()
    {
        var expected = new List<UserConversation>();
        foreach (var conversation in _conversationList)
        {
            await _store.CreateConversation(conversation);
            expected.Add(conversation);
        }

        var parameters = new GetConversationsParameters(_conversationList[0].Username, 100, "", 0);
        var actual = await _store.GetConversations(parameters);
        Assert.Equivalent(expected, actual.Conversations);
    }
    
    
    [Fact]
    public async Task GetConversationMessages_WithContinuationToken()
    {
        foreach (var conversation in _conversationList)
        {
            await _store.CreateConversation(conversation);
        }

        var parametersInitialCall = new GetConversationsParameters(_conversationList[0].Username, 2, "", 0);
        var actualInitialCall = await _store.GetConversations(parametersInitialCall);
        Assert.Equivalent(_conversationList[0], actualInitialCall.Conversations[0]);
        Assert.Equivalent(_conversationList[1], actualInitialCall.Conversations[1]);
        var parametersSecondCall = new GetConversationsParameters(_conversationList[0].Username, 2, actualInitialCall.ContinuationToken, 0);
        var actualSecondCall = await _store.GetConversations(parametersSecondCall);
        Assert.Equivalent(_conversationList[2], actualSecondCall.Conversations[0]);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(150, 3)]
    [InlineData(2, 2)]
    [InlineData(null, 1)]
    public async Task GetConversationMessages_WithBadLimit(int limit, int actualCount)
    {
        foreach (var conversation in _conversationList)
        {
            await _store.CreateConversation(conversation);
        }
        var parameters = new GetConversationsParameters(_conversationList[0].Username, limit, "", 0);
        var actual = await _store.GetConversations(parameters);
        Assert.Equal(actualCount, actual.Conversations.Count);
    }
    
    [Fact]
    public async Task GetConversationMessages_WithBadContinuationToken()
    {
        foreach (var conversation in _conversationList)
        {
            await _store.CreateConversation(conversation);
        }
        await Assert.ThrowsAsync<CosmosException>( async () =>
        {
            var parameters = new GetConversationsParameters(_conversationList[0].Username, 100, "bad token", 0);
            var actual = await _store.GetConversations(parameters);
        });
    }
    
    [Fact]
    public async Task GetConversationMessages_WithUnixTime()
    {
        foreach (var conversation in _conversationList)
        {
            await _store.CreateConversation(conversation);
        }
        var parameters = new GetConversationsParameters(_conversationList[0].Username, 100, "", 1000);
        var actual = await _store.GetConversations(parameters);
        Assert.Equivalent(_conversationList[0], actual.Conversations[0]);
        Assert.Equivalent(_conversationList[1], actual.Conversations[1]);
    }
}