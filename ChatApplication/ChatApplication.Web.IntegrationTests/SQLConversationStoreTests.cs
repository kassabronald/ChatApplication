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

public class SQLConversationStoreTests: IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly IConversationStore _conversationStore;
    private readonly IProfileStore _profileStore;
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
            await _conversationStore.DeleteUserConversation(conversation);
        }
        foreach(var profile in new List<Profile>(){_profile1, _profile2, _profile3, _profile4})
        {
            try
            {
                await _profileStore.DeleteProfile(profile.Username);
            }
            catch
            {
                
            }
        }
    }
    
    public SQLConversationStoreTests(WebApplicationFactory<Program> factory)
    {
        List<Profile> recipients1 = new() { _profile2 };
        List<Profile> recipients2 = new() { _profile3 };
        List<Profile> recipients3 = new() {  _profile4 };
        _conversation1 = new UserConversation(Guid.NewGuid().ToString(), recipients1, 1002, _profile1.Username);
        _conversation2 = new UserConversation(Guid.NewGuid().ToString(), recipients2, 1001, _profile1.Username);
        _conversation3 = new UserConversation(Guid.NewGuid().ToString(), recipients3, 1000, _profile1.Username);
        _conversationList = new List<UserConversation>(){_conversation1, _conversation2, _conversation3};
        
        var services = factory.Services;
        var sqlSettings = services.GetRequiredService<IOptions<SQLSettings>>();
        _conversationStore = new SQLConversationStore(sqlSettings);
        _profileStore = new SQLProfileStore(sqlSettings);
    }
    
    [Fact]
    public async Task GetUserConversation_Success()
    {
        await _profileStore.AddProfile(_profile1);
        await _profileStore.AddProfile(_profile2);
        await _conversationStore.CreateUserConversation(_conversationList[0]);
        var conversation = await _conversationStore.GetUserConversation(_conversationList[0].Username, _conversationList[0].ConversationId);
        Assert.Equivalent(_conversationList[0], conversation);
    }
    
    [Fact]
    public async Task GetUserConversation_NotFoundUsername()
    {
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            var randomId = Guid.NewGuid().ToString();
            await _conversationStore.GetUserConversation(randomId, _conversationList[0].ConversationId);
        });
    }
    
    [Fact]
    public async Task GetUserConversation_NotFoundConversationId()
    {
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            var randomId = Guid.NewGuid().ToString();
            await _conversationStore.GetUserConversation(_conversationList[0].Username, randomId);
        });
    }
    
    [Fact]
    
    public async Task GetUserConversation_EmptyConversationId()
    {
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            await _conversationStore.GetUserConversation(_conversationList[0].Username, "");
        });
    }
    
    [Fact]
    public async Task UpdateConversationLastMessageTime_Success()
    {
        await _profileStore.AddProfile(_profile1);
        await _profileStore.AddProfile(_profile2);
        var receiverConversation = new UserConversation(_conversationList[0].ConversationId, new List<Profile>{_profile1}, _conversationList[0].LastMessageTime, _conversationList[0].Recipients[0].Username);
        var senderConversation = _conversationList[0];
        await _conversationStore.CreateUserConversation(senderConversation);
        await _conversationStore.CreateUserConversation(receiverConversation);
        await _conversationStore.UpdateConversationLastMessageTime(senderConversation, 1005);
        var senderConversationAfterUpdate = await _conversationStore.GetUserConversation(senderConversation.Username,senderConversation.ConversationId);
        var receiverConversationAfterUpdate = await _conversationStore.GetUserConversation(receiverConversation.Username, receiverConversation.ConversationId);
        Assert.Equal(1005, senderConversationAfterUpdate.LastMessageTime);
        Assert.Equal(1005, receiverConversationAfterUpdate.LastMessageTime);
    }
    
    [Fact]
    
    public async Task UpdateConversationLastMessageTime_ConversationNotFound()
    {
        
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            await _conversationStore.UpdateConversationLastMessageTime(_conversationList[0], 1005);
        });
    }

    [Fact]

    public async Task CreateUserConversation_Success()
    {
        await _profileStore.AddProfile(_profile1);
        await _profileStore.AddProfile(_profile2);
        await _conversationStore.CreateUserConversation(_conversationList[0]);
        var conversation = await _conversationStore.GetUserConversation(_conversationList[0].Username, _conversationList[0].ConversationId);
        Assert.Equivalent(_conversationList[0], conversation);
    }
    

    [Fact]

    public async Task CreateUserConversation_EmptyId()
    {
        var conversation = new UserConversation("", new List<Profile>(), 1000, _conversationList[0].ConversationId);
        await Assert.ThrowsAsync<SqlException>(async () =>
        {
            await _conversationStore.CreateUserConversation(conversation);
        });
    }


    [Fact]

    public async Task DeleteUserConversation_Success()
    {
        await _profileStore.AddProfile(_profile1);
        await _profileStore.AddProfile(_profile2);
        await _conversationStore.CreateUserConversation(_conversationList[0]);
        await _conversationStore.DeleteUserConversation(_conversationList[0]);
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            await _conversationStore.GetUserConversation(_conversationList[0].Username, _conversationList[0].ConversationId);
        });
    }

    [Fact]

    public async Task GetAllConversations_Success()
    {
        await _profileStore.AddProfile(_profile1);
        await _profileStore.AddProfile(_profile2);
        await _profileStore.AddProfile(_profile3);
        await _profileStore.AddProfile(_profile4);
        var expected = new List<UserConversation>();
        foreach (var conversation in _conversationList)
        {
            await _conversationStore.CreateUserConversation(conversation);
            expected.Add(conversation);
        }

        var parameters = new GetConversationsParameters(_conversationList[0].Username, 100, "", 0);
        var actual = await _conversationStore.GetConversations(parameters);
        Assert.Equivalent(expected, actual.Conversations);
    }
    
    
    [Fact]
    public async Task GetConversationMessages_WithContinuationToken()
    {
        await _profileStore.AddProfile(_profile1);
        await _profileStore.AddProfile(_profile2);
        await _profileStore.AddProfile(_profile3);
        await _profileStore.AddProfile(_profile4);
        
        foreach (var conversation in _conversationList)
        {
            await _conversationStore.CreateUserConversation(conversation);
        }

        var parametersInitialCall = new GetConversationsParameters(_conversationList[0].Username, 2, "", 0);
        var actualInitialCall = await _conversationStore.GetConversations(parametersInitialCall);
        Assert.Equivalent(_conversationList[0], actualInitialCall.Conversations[0]);
        Assert.Equivalent(_conversationList[1], actualInitialCall.Conversations[1]);
        var parametersSecondCall = new GetConversationsParameters(_conversationList[0].Username, 2, actualInitialCall.ContinuationToken, 0);
        var actualSecondCall = await _conversationStore.GetConversations(parametersSecondCall);
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
        await _profileStore.AddProfile(_profile1);
        await _profileStore.AddProfile(_profile2);
        await _profileStore.AddProfile(_profile3);
        await _profileStore.AddProfile(_profile4);
        
        foreach (var conversation in _conversationList)
        {
            await _conversationStore.CreateUserConversation(conversation);
        }
        var parameters = new GetConversationsParameters(_conversationList[0].Username, limit, "", 0);
        var actual = await _conversationStore.GetConversations(parameters);
        Assert.Equal(actualCount, actual.Conversations.Count);
    }
    
    [Fact]
    public async Task GetConversationMessages_WithBadContinuationToken()
    {
        await _profileStore.AddProfile(_profile1);
        await _profileStore.AddProfile(_profile2);
        await _profileStore.AddProfile(_profile3);
        await _profileStore.AddProfile(_profile4);
        foreach (var conversation in _conversationList)
        {
            await _conversationStore.CreateUserConversation(conversation);
        }
        await Assert.ThrowsAsync<ArgumentException>( async () =>
        {
            var parameters = new GetConversationsParameters(_conversationList[0].Username, 100, "bad token", 0);
            var actual = await _conversationStore.GetConversations(parameters);
        });
    }
    
    [Fact]
    public async Task GetConversationMessages_WithUnixTime()
    {
        await _profileStore.AddProfile(_profile1);
        await _profileStore.AddProfile(_profile2);
        await _profileStore.AddProfile(_profile3);
        await _profileStore.AddProfile(_profile4);
        foreach (var conversation in _conversationList)
        {
            await _conversationStore.CreateUserConversation(conversation);
        }
        var parameters = new GetConversationsParameters(_conversationList[0].Username, 100, "", 1000);
        var actual = await _conversationStore.GetConversations(parameters);
        Assert.Equivalent(_conversationList[0], actual.Conversations[0]);
        Assert.Equivalent(_conversationList[1], actual.Conversations[1]);
    }
}