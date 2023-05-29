using System.Net;
using System.Text;
using ChatApplication.Exceptions;
using ChatApplication.Exceptions.ConversationParticipantsExceptions;
using ChatApplication.Exceptions.StorageExceptions;
using ChatApplication.Services;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;

namespace ChatApplication.Web.Tests.Controllers;

public class ConversationControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly Mock<IConversationService> _conversationServiceMock;
    private readonly HttpClient _httpClient;
    
    public ConversationControllerTests(WebApplicationFactory<Program> factory)
    {
        _conversationServiceMock = new Mock<IConversationService>();
        _httpClient = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services => { services.AddSingleton(_conversationServiceMock.Object); });
        }).CreateClient();
    }

    
    [Fact]
    public async Task AddMessage_Success_201()
    {
        var messageRequest = new SendMessageRequest("1234", "ronald", "hey bro wanna hit the gym");
        const string conversationId = "456";
        var jsonContent = new StringContent(JsonConvert.SerializeObject(messageRequest), Encoding.Default, "application/json");
        var response = await _httpClient.PostAsync($"/api/Conversations/{conversationId}/messages", jsonContent);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"http://localhost/api/Conversations/{conversationId}/messages", response.Headers.GetValues("Location").First());
    }


    [Theory]
    [InlineData("", "ronald", "hey bro wanna hit the gym", "456")]
    [InlineData("1234", "", "hey bro wanna hit the gym", "456")]
    [InlineData("1234", "ronald", "", "456")]
    [InlineData(" ", "ronald", "hey bro wanna hit the gym", "456")]
    [InlineData("1234", " ", "hey bro wanna hit the gym", "456")]
    [InlineData("1234", "ronald", " ", "456")]
    [InlineData("1234", "ronald", "hey bro wanna hit the gym", " ")]
    [InlineData(null, "ronald", "hey bro wanna hit the gym", "456")]
    [InlineData("1234", null, "hey bro wanna hit the gym", "456")]
    [InlineData("1234", "ronald", null, "456")]

    public async Task AddMessage_InvalidArguments_400(string messageId, string senderUsername, string messageContent,
        string conversationId)
    {
        var messageRequest = new SendMessageRequest(messageId, senderUsername, messageContent);
        var message = new Message(messageId, senderUsername, conversationId, messageContent,1000);
        var jsonContent = new StringContent(JsonConvert.SerializeObject(messageRequest), Encoding.Default, "application/json");
        var response = await _httpClient.PostAsync($"/api/Conversations/{conversationId}/messages", jsonContent);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        _conversationServiceMock.Verify(mock => mock.AddMessage(message), Times.Never);
    }

    [Fact]

    public async Task AddMessage_ConversationNotFound_404()
    {
        var messageRequest = new SendMessageRequest("1234", "ronald", "hey bro wanna hit the gym");
        const string conversationId = "456";
        
        _conversationServiceMock.Setup(x => x.EnqueueAddMessage(
            It.Is<Message>(m =>
                m.MessageId == messageRequest.Id &&
                m.SenderUsername == messageRequest.SenderUsername &&
                m.Text == messageRequest.Text &&
                m.ConversationId == conversationId
            ))).ThrowsAsync(new ConversationNotFoundException(conversationId));
        
        var jsonContent = new StringContent(JsonConvert.SerializeObject(messageRequest), Encoding.Default, "application/json");
        var response = await _httpClient.PostAsync($"/api/Conversations/{conversationId}/messages", jsonContent);
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }


    [Fact]
    public async Task AddMessage_MessageAlreadyExists_409()
    {
        var messageRequest = new SendMessageRequest("1234", "ronald", "hey bro wanna hit the gym");
        const string conversationId = "456";
        var message = new Message(messageRequest.Id, messageRequest.SenderUsername, conversationId, messageRequest.Text, 1000);
        
        _conversationServiceMock.Setup(x => x.EnqueueAddMessage(
            It.Is<Message>(m =>
                m.MessageId == messageRequest.Id &&
                m.SenderUsername == messageRequest.SenderUsername &&
                m.Text == messageRequest.Text &&
                m.ConversationId == conversationId
            ))).ThrowsAsync(new MessageAlreadyExistsException(message.MessageId));
        
        var jsonContent = new StringContent(JsonConvert.SerializeObject(messageRequest), Encoding.Default, "application/json");
        var response = await _httpClient.PostAsync($"/api/Conversations/{conversationId}/messages", jsonContent);
        
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]

    public async Task AddMessage_StorageUnavailable_503()
    {
        var messageRequest = new SendMessageRequest("1234", "ronald", "hey bro wanna hit the gym");
        const string conversationId = "456";
        
        _conversationServiceMock.Setup(x => x.EnqueueAddMessage(
            It.Is<Message>(m =>
                m.MessageId == messageRequest.Id &&
                m.SenderUsername == messageRequest.SenderUsername &&
                m.Text == messageRequest.Text &&
                m.ConversationId == conversationId
            ))).ThrowsAsync(new StorageUnavailableException("database is down"));
        
        var jsonContent = new StringContent(JsonConvert.SerializeObject(messageRequest), Encoding.Default, "application/json");
        var response = await _httpClient.PostAsync($"/api/Conversations/{conversationId}/messages", jsonContent);
        
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task StartConversation_Success_201()
    {
        var messageRequest = new SendMessageRequest("12345", "Ronald", "Haha Bro farex");
        var participants = new List<string> {"Ronald", "Farex"};
        var conversationRequest = new StartConversationRequest(participants, messageRequest);

        _conversationServiceMock.Setup(x => x.EnqueueStartConversation(
            It.Is<StartConversationParameters>(r =>  
                r.participants.SequenceEqual(participants) && r.messageContent == messageRequest.Text &&
                r.messageId == messageRequest.Id && r.senderUsername == messageRequest.SenderUsername
            ))).ReturnsAsync("_Ronald_Farex");
        
        var jsonContent = new StringContent(JsonConvert.SerializeObject(conversationRequest), Encoding.Default, "application/json");
        var response = await  _httpClient.PostAsync("/api/Conversations", jsonContent);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("http://localhost/api/Conversations?username=Ronald", response.Headers.GetValues("Location").First());
        
        var responseString = await response.Content.ReadAsStringAsync();
        var answer = JsonConvert.DeserializeObject<StartConversationResponse>(responseString);
        
        Assert.Equal("_Ronald_Farex", answer.Id);
        
        _conversationServiceMock.Verify(mock => mock.EnqueueStartConversation(
            It.Is<StartConversationParameters>(r =>  
                r.participants.SequenceEqual(participants) && r.messageContent == messageRequest.Text &&
                r.messageId == messageRequest.Id && r.senderUsername == messageRequest.SenderUsername
            )), Times.Once);
    }

    [Fact]

    public async Task StartConversation_LessThanTwoParticipants_400()
    {
        var messageRequest = new SendMessageRequest("12345", "Ronald", "Haha Bro farex");
        var participants = new List<string> {"Ronald"};
        var conversationRequest = new StartConversationRequest(participants, messageRequest);
        
        var jsonContent = new StringContent(JsonConvert.SerializeObject(conversationRequest), Encoding.Default, "application/json");
        var response = await _httpClient.PostAsync("/api/Conversations", jsonContent);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StartConversation_SenderUsernameNotInParticipants_400()
    {
        var messageRequest = new SendMessageRequest("12345", "Ronald", "Haha Bro farex");
        var participants = new List<string> { "Farex", "Messi" };
        var conversationRequest = new StartConversationRequest(participants, messageRequest);

        _conversationServiceMock.Setup(x => x.EnqueueStartConversation(
            It.Is<StartConversationParameters>(r =>
                r.participants.SequenceEqual(participants) && r.messageContent == messageRequest.Text &&
                r.messageId == messageRequest.Id && r.senderUsername == messageRequest.SenderUsername)))
            .ThrowsAsync(new SenderNotFoundException(messageRequest.SenderUsername));

        var jsonContent = new StringContent(JsonConvert.SerializeObject(conversationRequest),
            Encoding.Default, "application/json");

        var response = await _httpClient.PostAsync("api/Conversations", jsonContent);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StartConversation_ProfileNotFound_404()
    {
        var messageRequest = new SendMessageRequest("12345", "Ronald", "Haha Bro farex");
        var participants = new List<string> { "Ronald", "Farex" };
        var conversationRequest = new StartConversationRequest(participants, messageRequest);

        _conversationServiceMock.Setup(x => x.EnqueueStartConversation(
            It.Is<StartConversationParameters>(r =>
                r.participants.SequenceEqual(participants) && r.messageContent == messageRequest.Text &&
                r.messageId == messageRequest.Id && r.senderUsername == messageRequest.SenderUsername)))
            .ThrowsAsync(new ProfileNotFoundException("Profile does not exist"));

        var jsonContent = new StringContent(JsonConvert.SerializeObject(conversationRequest),
            Encoding.Default, "application/json");

        var response = await _httpClient.PostAsync("/api/Conversations", jsonContent);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StartConversation_MessageAlreadyExists_409()
    {
        var messageRequest = new SendMessageRequest("12345", "Ronald", "Haha Bro farex");
        var participants = new List<string> { "Ronald", "Farex" };
        var conversationRequest = new StartConversationRequest(participants, messageRequest);

        _conversationServiceMock.Setup(x => x.EnqueueStartConversation(
            It.Is<StartConversationParameters>(r =>
                r.participants.SequenceEqual(participants) && r.messageContent == messageRequest.Text &&
                r.messageId == messageRequest.Id && r.senderUsername == messageRequest.SenderUsername)))
            .ThrowsAsync(new MessageAlreadyExistsException("Message already exists"));

        var jsonContent = new StringContent(JsonConvert.SerializeObject(conversationRequest),
            Encoding.Default, "application/json");

        var response = await _httpClient.PostAsync("/api/Conversations", jsonContent);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]

    public async Task StartConversation_DuplicateParticipant_400()
    {
        var messageRequest = new SendMessageRequest("12345", "Ronald", "Haha Bro farex");
        var participants = new List<string> { "Ronald", "Farex", "Ronald" };
        var conversationRequest = new StartConversationRequest(participants, messageRequest);

        _conversationServiceMock.Setup(x => x.EnqueueStartConversation(
                It.Is<StartConversationParameters>(r =>
                    r.participants.SequenceEqual(participants) && r.messageContent == messageRequest.Text &&
                    r.messageId == messageRequest.Id && r.senderUsername == messageRequest.SenderUsername)))
            .ThrowsAsync(new DuplicateParticipantException("Participant is duplicated"));

        var jsonContent = new StringContent(JsonConvert.SerializeObject(conversationRequest),
            Encoding.Default, "application/json");

        var response = await _httpClient.PostAsync("/api/Conversations", jsonContent);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]

    public async Task StartConversation_StorageNotAvailable_503()
    {
        var messageRequest = new SendMessageRequest("12345", "Ronald", "Haha Bro farex");
        var participants = new List<string> { "Ronald", "Farex" };
        var conversationRequest = new StartConversationRequest(participants, messageRequest);

        _conversationServiceMock.Setup(x => x.EnqueueStartConversation(
                It.Is<StartConversationParameters>(r =>
                    r.participants.SequenceEqual(participants) && r.messageContent == messageRequest.Text &&
                    r.messageId == messageRequest.Id && r.senderUsername == messageRequest.SenderUsername)))
            .ThrowsAsync(new StorageUnavailableException("database is down"));
        
        var jsonContent = new StringContent(JsonConvert.SerializeObject(conversationRequest), Encoding.Default, "application/json");
        var response = await _httpClient.PostAsync("/api/Conversations", jsonContent);
        
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        
    }

    [Fact]
    public async Task GetConversationMessages_Success_200()
    {
        const string conversationId = "_Farex_Ronald";
        const string nextContinuationToken = "frfr";
        var messages = new List<ConversationMessage>
        {
            new("12345", "Farex", 0),
            new("12346", "Ronald", 1)
        };
        var parameters = new GetMessagesParameters(conversationId, 50, "", 0);

        _conversationServiceMock
            .Setup(x => x.GetMessages(parameters))
            .ReturnsAsync(new GetMessagesResult(messages, nextContinuationToken));

        const string expectedNextUri = $"/api/Conversations/{conversationId}/messages?limit=50&continuationToken={nextContinuationToken}&lastSeenMessageTime=0";
        const string uri = $"/api/conversations/{conversationId}/messages/";
        var response = await _httpClient.GetAsync(uri);
        var responseString = await response.Content.ReadAsStringAsync();
        var getConversationMessagesResponseReceived = JsonConvert.DeserializeObject<GetMessagesResponse>(responseString);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(messages, getConversationMessagesResponseReceived.Messages);
        Assert.Equal(expectedNextUri, getConversationMessagesResponseReceived.NextUri);
    }

    [Fact]

    public async Task GetAllMessages_StorageUnavailable_503()
    {
        const string conversationId = "_Farex_Ronald";
        var parameters = new GetMessagesParameters(conversationId, 50, "", 0);

        _conversationServiceMock
            .Setup(x => x.GetMessages(parameters))
            .ThrowsAsync(new StorageUnavailableException("database is down"));

        const string uri = $"/api/conversations/{conversationId}/messages/";
        var response = await _httpClient.GetAsync(uri);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task GetAllConversations_Success_200()
    {
        const string username = "jad";
        const string nextContinuationToken = "frfr";
        var recipients1 = new List<Profile>{new("karim", "karim", "haddad", "1234")};
        var recipients2 = new List<Profile>{new("ronald", "ronald", "haddad", "1234")};
        var conversation1 = new UserConversation("_jad_ronald", recipients1, 1000, "jad");
        var conversation2 = new UserConversation("_jad_karim", recipients2, 1001, "jad");
        var conversations = new List<UserConversation> { conversation1, conversation2 };
        var conversationsMetadata = conversations.Select(conversation => new ConversationMetaData(conversation.ConversationId, conversation.LastMessageTime, conversation.Recipients[0])).ToList();

        var parameters = new GetConversationsParameters(username, 50, "", 0);
        _conversationServiceMock
            .Setup(x => x.GetConversations(parameters))
            .ReturnsAsync(new GetConversationsResult(conversations, nextContinuationToken));

        const string expectedNextUri = $"/api/Conversations?username={username}&limit=50&continuationToken={nextContinuationToken}&lastSeenConversationTime=0";
        const string uri = $"/api/conversations?username={username}";
        var response = await _httpClient.GetAsync(uri);
        var responseString = await response.Content.ReadAsStringAsync();
        var getAllConversationsResponseReceived =
            JsonConvert.DeserializeObject<GetConversationsResponse>(responseString);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equivalent(conversationsMetadata, getAllConversationsResponseReceived.Conversations);
        Assert.Equal(expectedNextUri, getAllConversationsResponseReceived.NextUri);
    }

    [Fact]

    public async Task GetAllConversations_StorageUnavailable_503()
    {

        const string username = "Ronald";
        
        var parameters = new GetConversationsParameters(username, 50, "", 0);
        _conversationServiceMock
            .Setup(x => x.GetConversations(parameters))
            .ThrowsAsync(new StorageUnavailableException("database is down"));
        
        const string uri = $"/api/conversations?username={username}";
        var response = await _httpClient.GetAsync(uri);
        
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

}