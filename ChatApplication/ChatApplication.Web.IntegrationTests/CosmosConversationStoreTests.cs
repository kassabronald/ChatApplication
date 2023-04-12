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
            await _store.DeleteUserConversation(conversation);
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

    public async Task GetUserConversation()
    {
        await _store.CreateUserConversation(_conversationList[0]);
        var conversation = await _store.GetUserConversation(_conversationList[0].Username, _conversationList[0].ConversationId);
        Assert.Equivalent(_conversationList[0], conversation);
    }
    
    [Fact]
    
    public async Task GetUserConversation_NotFoundUsername()
    {
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            var randomId = Guid.NewGuid().ToString();
            await _store.GetUserConversation(randomId, _conversationList[0].ConversationId);
        });
    }
    
    [Fact]
    
    public async Task GetUserConversation_NotFoundConversationId()
    {
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            var randomId = Guid.NewGuid().ToString();
            await _store.GetUserConversation(_conversationList[0].Username, randomId);
        });
    }

    [Fact]
    
    public async Task GetUserConversation_EmptyConversationId()
    {
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.GetUserConversation(_conversationList[0].Username, "");
        });
    }
    
    [Fact]
    
    public async Task UpdateConversationLastMessageTime()
    {
        var receiverConversation = new UserConversation(_conversationList[0].ConversationId, _conversationList[0].Participants, _conversationList[0].LastMessageTime, _conversationList[0].Participants[1].Username);
        var senderConversation = _conversationList[0];
        await _store.CreateUserConversation(senderConversation);
        await _store.CreateUserConversation(receiverConversation);
        await _store.UpdateConversationLastMessageTime(_conversationList[0], 1005);
        var senderConversationAfterUpdate = await _store.GetUserConversation(senderConversation.Username,senderConversation.ConversationId);
        var receiverConversationAfterUpdate = await _store.GetUserConversation(receiverConversation.Username, receiverConversation.ConversationId);
        Assert.Equal(1005, senderConversationAfterUpdate.LastMessageTime);
        Assert.Equal(1005, receiverConversationAfterUpdate.LastMessageTime);
    }
    
    [Fact]
    
    public async Task UpdateConversationLastMessageTime_NotFound()
    {
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            await _store.UpdateConversationLastMessageTime(_conversationList[0], 1001);
        });
    }

    [Fact]

    public async Task CreateUserConversation()
    {
        await _store.CreateUserConversation(_conversationList[0]);
        var conversation = await _store.GetUserConversation(_conversationList[0].Username, _conversationList[0].ConversationId);
        Assert.Equivalent(_conversationList[0], conversation);
    }
    
    [Fact]
    //TODO: Change this to not throw exception
    
    public async Task CreateUserConversation_Conflict()
    {
        await _store.CreateUserConversation(_conversationList[0]);
        await Assert.ThrowsAsync<ConversationAlreadyExistsException>(async () =>
        {
            await _store.CreateUserConversation(_conversationList[0]);
        });
    }

    [Fact]

    public async Task CreateUserConversation_EmptyId()
    {
        var conversation = new UserConversation("", new List<Profile>(), 1000, _conversationList[0].ConversationId);
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.CreateUserConversation(conversation);
        });
    }


    [Fact]

    public async Task DeleteUserConversation()
    {
        await _store.CreateUserConversation(_conversationList[0]);
        await _store.DeleteUserConversation(_conversationList[0]);
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            await _store.GetUserConversation(_conversationList[0].Username, _conversationList[0].ConversationId);
        });
    }

    [Fact]

    public async Task DeleteUserConversation_EmptyId()
    {
        var conversation = new UserConversation("", new List<Profile>(), 1000, _conversationList[0].ConversationId);
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.DeleteUserConversation(conversation);
        });
    }
    
    [Fact]

    public async Task GetAllConversations()
    {
        var expected = new List<UserConversation>();
        foreach (var conversation in _conversationList)
        {
            await _store.CreateUserConversation(conversation);
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
            await _store.CreateUserConversation(conversation);
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
            await _store.CreateUserConversation(conversation);
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
            await _store.CreateUserConversation(conversation);
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
            await _store.CreateUserConversation(conversation);
        }
        var parameters = new GetConversationsParameters(_conversationList[0].Username, 100, "", 1000);
        var actual = await _store.GetConversations(parameters);
        Assert.Equivalent(_conversationList[0], actual.Conversations[0]);
        Assert.Equivalent(_conversationList[1], actual.Conversations[1]);
    }
}