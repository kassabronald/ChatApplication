using System.Net;
using System.Text;
using ChatApplication.Exceptions;
using ChatApplication.Services;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Cosmos;
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
    public async Task AddMessage()
    {
        var messageRequest = new MessageRequest("1234", "ronald", "hey bro wanna hit the gym");
        string conversationId = "456";
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

    public async Task AddMessage_InvalidArguments(string messageId, string senderUsername, string messageContent,
        string conversationId)
    {
        var messageRequest = new MessageRequest(messageId, senderUsername, messageContent);
        var message = new Message(messageId, senderUsername, messageContent,1000, conversationId);
        var jsonContent = new StringContent(JsonConvert.SerializeObject(messageRequest), Encoding.Default, "application/json");
        var response = await _httpClient.PostAsync($"/api/Conversations/{conversationId}/messages", jsonContent);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _conversationServiceMock.Verify(mock => mock.AddMessage(message), Times.Never);
    }

    [Fact]

    public async Task AddMessage_ConversationNotFound()
    {
        var messageRequest = new MessageRequest("1234", "ronald", "hey bro wanna hit the gym");
        string conversationId = "456";
        _conversationServiceMock.Setup(x => x.AddMessage(
            It.Is<Message>(m =>
                m.MessageId == messageRequest.MessageId &&
                m.SenderUsername == messageRequest.SenderUsername &&
                m.MessageContent == messageRequest.MessageContent &&
                m.ConversationId == conversationId
            ))).ThrowsAsync(new ConversationNotFoundException(conversationId));
        var jsonContent = new StringContent(JsonConvert.SerializeObject(messageRequest), Encoding.Default, "application/json");
        var response = await _httpClient.PostAsync($"/api/Conversations/{conversationId}/messages", jsonContent);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }


    [Fact]

    public async Task AddMessage_MessageAlreadyExists()
    {
        var messageRequest = new MessageRequest("1234", "ronald", "hey bro wanna hit the gym");
        string conversationId = "456";
        var message = new Message(messageRequest.MessageId, messageRequest.SenderUsername, messageRequest.MessageContent,1000, conversationId);
        _conversationServiceMock.Setup(x => x.AddMessage(
            It.Is<Message>(m =>
                m.MessageId == messageRequest.MessageId &&
                m.SenderUsername == messageRequest.SenderUsername &&
                m.MessageContent == messageRequest.MessageContent &&
                m.ConversationId == conversationId
            ))).ThrowsAsync(new MessageAlreadyExistsException(message.MessageId));
        var jsonContent = new StringContent(JsonConvert.SerializeObject(messageRequest), Encoding.Default, "application/json");
        var response = await _httpClient.PostAsync($"/api/Conversations/{conversationId}/messages", jsonContent);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddMessage_CosmosException()
    {
        var messageRequest = new MessageRequest("1234", "ronald", "hey bro wanna hit the gym");
        string conversationId = "456";
        _conversationServiceMock.Setup(x => x.AddMessage(
            It.Is<Message>(m =>
                m.MessageId == messageRequest.MessageId &&
                m.SenderUsername == messageRequest.SenderUsername &&
                m.MessageContent == messageRequest.MessageContent &&
                m.ConversationId == conversationId
            ))).ThrowsAsync(new CosmosException("error", HttpStatusCode.BadRequest, 0, "error", 0));
        var jsonContent = new StringContent(JsonConvert.SerializeObject(messageRequest), Encoding.Default, "application/json");
        var response = await _httpClient.PostAsync($"/api/Conversations/{conversationId}/messages", jsonContent);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]

    public async Task CreateConversation()
    {
        var messageRequest = new MessageRequest("12345", "Ronald", "Haha Bro farex");
        var participants = new List<string> {"Ronald", "Farex"};
        var conversationRequest = new StartConversationRequest(participants, messageRequest);
        _conversationServiceMock.Setup(x=> x.StartConversation(
            messageRequest.MessageId, 
            messageRequest.SenderUsername, 
            messageRequest.MessageContent, 
            It.IsAny<long>(), 
            participants)).ReturnsAsync("_Ronald_Farex");
        var jsonContent = new StringContent(JsonConvert.SerializeObject(conversationRequest), Encoding.Default, "application/json");
        var response = await  _httpClient.PostAsync("/api/Conversations", jsonContent);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("http://localhost/api/Conversations/Ronald", response.Headers.GetValues("Location").First());
        var responseString = await response.Content.ReadAsStringAsync();
        var answer = JsonConvert.DeserializeObject<StartConversationResponse>(responseString);
        Assert.Equal("_Ronald_Farex", answer.ConversationId);
        _conversationServiceMock.Verify(mock => mock.StartConversation(
            messageRequest.MessageId, 
            messageRequest.SenderUsername, 
            messageRequest.MessageContent, 
            It.IsAny<long>(), 
            participants), Times.Once);
    }

    [Fact]

    public async Task StartConversation_LessThanTwoParticipants()
    {
        var messageRequest = new MessageRequest("12345", "Ronald", "Haha Bro farex");
        var participants = new List<string> {"Ronald"};
        var conversationRequest = new StartConversationRequest(participants, messageRequest);
        var jsonContent = new StringContent(JsonConvert.SerializeObject(conversationRequest), Encoding.Default, "application/json");
        var response = await _httpClient.PostAsync("/api/Conversations", jsonContent);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]

    public async Task StartConversation_SenderUsernameNotInParticipants()
    {
        var messageRequest = new MessageRequest("12345", "Ronald", "Haha Bro farex");
        var participants = new List<string> {"Farex", "Messi"};
        var conversationRequest = new StartConversationRequest(participants, messageRequest);
        _conversationServiceMock.Setup(x=> x.StartConversation(messageRequest.MessageId, 
            messageRequest.SenderUsername, messageRequest.MessageContent, It.IsAny<long>(), participants)).
            ThrowsAsync(new ProfileNotFoundException(messageRequest.SenderUsername));
        var jsonContent = new StringContent(JsonConvert.SerializeObject(conversationRequest), Encoding.Default, "application/json");
        var response = await _httpClient.PostAsync("api/Conversations", jsonContent);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]

    public async Task StartConversation_ProfileNotFound()
    {
        var messageRequest = new MessageRequest("12345", "Ronald", "Haha Bro farex");
        var participants = new List<string> {"Ronald", "Farex"};
        var conversationRequest = new StartConversationRequest(participants, messageRequest);
        _conversationServiceMock.Setup(x=> x.StartConversation(
            messageRequest.MessageId, 
            messageRequest.SenderUsername, 
            messageRequest.MessageContent, 
            It.IsAny<long>(), 
            participants)).ThrowsAsync(new ProfileNotFoundException());
        var jsonContent = new StringContent(JsonConvert.SerializeObject(conversationRequest), Encoding.Default, "application/json");
        var response = await  _httpClient.PostAsync("/api/Conversations", jsonContent);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]

    public async Task StartConversation_MessageAlreadyExists()
    {
        var messageRequest = new MessageRequest("12345", "Ronald", "Haha Bro farex");
        var participants = new List<string> {"Ronald", "Farex"};
        var conversationRequest = new StartConversationRequest(participants, messageRequest);
        _conversationServiceMock.Setup(x=> x.StartConversation(
            messageRequest.MessageId, 
            messageRequest.SenderUsername, 
            messageRequest.MessageContent, 
            It.IsAny<long>(), 
            participants)).ThrowsAsync(new MessageAlreadyExistsException());
        var jsonContent = new StringContent(JsonConvert.SerializeObject(conversationRequest), Encoding.Default, "application/json");
        var response = await  _httpClient.PostAsync("/api/Conversations", jsonContent);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
    
    [Fact]

    public async Task StartConversation_ConversationAlreadyExists()
    {
        var messageRequest = new MessageRequest("12345", "Ronald", "Haha Bro farex");
        var participants = new List<string> {"Ronald", "Farex"};
        var conversationRequest = new StartConversationRequest(participants, messageRequest);
        _conversationServiceMock.Setup(x=> x.StartConversation(
            messageRequest.MessageId, 
            messageRequest.SenderUsername, 
            messageRequest.MessageContent, 
            It.IsAny<long>(), 
            participants)).ThrowsAsync(new ConversationAlreadyExistsException());
        var jsonContent = new StringContent(JsonConvert.SerializeObject(conversationRequest), Encoding.Default, "application/json");
        var response = await  _httpClient.PostAsync("/api/Conversations", jsonContent);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]

     public async Task GetConversationMessages()
     {
         var conversationId = "_Farex_Ronald";
         var nextContinuationToken = "frfr";
         var messages = new List<ConversationMessage>
            {
                new("12345", "Farex", 0),
                new("12346", "Ronald", 1)
            };
         _conversationServiceMock
             .Setup(x => x.GetConversationMessages(conversationId, 50, "", 0))
             .ReturnsAsync(new ConversationMessageAndToken(messages, nextContinuationToken));
         var expectedNextUri = $"/api/Conversations/{conversationId}/messages?&limit=50&continuationToken={nextContinuationToken}&lastSeenMessageTime=0";
         var uri = $"/api/conversations/{conversationId}/messages/";
         var response = await _httpClient.GetAsync(uri);
         var responseString = await response.Content.ReadAsStringAsync();
         var getConversationMessagesResponseReceived = JsonConvert.DeserializeObject<GetConversationMessagesResponse>(responseString);
         Assert.Equal(HttpStatusCode.OK, response.StatusCode);
         Assert.Equal(messages, getConversationMessagesResponseReceived.Messages);
         Assert.Equal(expectedNextUri, getConversationMessagesResponseReceived.NextUri);
     }
     
     [Fact]

     public async Task GetAllConversations()
     {
         var username = "jad";
         var nextContinuationToken = "frfr";
         var participants1 = new List<Profile>();
         var participants2 = new List<Profile>();
         participants1.Add(new Profile("jad", "mike", "o hearn", "1234"));
         participants1.Add(new Profile("karim", "karim", "haddad", "1234"));
         participants2.Add(new Profile("jad", "mike", "o hearn", "1234"));
         participants2.Add(new Profile("ronald", "ronald", "haddad", "1234"));
         var conversation1 = new Conversation("_jad_ronald", participants1, 1000, "jad");
         var conversation2 = new Conversation("_jad_karim", participants2, 1001, "jad");
         var conversations = new List<Conversation> { conversation1, conversation2 };
         var conversationsMetadata = new List<ConversationMetaData>();
            foreach (var conversation in conversations)
            {
                var conversationMetaData = new ConversationMetaData(conversation.ConversationId, conversation.LastMessageTime, conversation.Participants);
                conversationsMetadata.Add(conversationMetaData);
            }
         _conversationServiceMock
             .Setup(x => x.GetAllConversations(username, 50, "", 0))
             .ReturnsAsync(new ConversationAndToken(conversations, nextContinuationToken));
         var expectedNextUri = $"/api/Conversations/{username}?&limit=50&continuationToken={nextContinuationToken}&lastSeenConversationTime=0";
         var uri = $"/api/conversations/{username}";
         var response = await _httpClient.GetAsync(uri);
         var responseString = await response.Content.ReadAsStringAsync();
         var getAllConversationsResponseReceived = JsonConvert.DeserializeObject<GetAllConversationsResponse>(responseString);
         Assert.Equal(HttpStatusCode.OK, response.StatusCode);
         Assert.Equivalent(conversationsMetadata, getAllConversationsResponseReceived.Conversations);
         Assert.Equal(expectedNextUri, getAllConversationsResponseReceived.NextUri);
     }
    
}