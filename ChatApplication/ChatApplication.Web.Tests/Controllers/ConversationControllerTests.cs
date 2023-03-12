using System.Net;
using System.Text;
using ChatApplication.Exceptions;
using ChatApplication.Services;
using ChatApplication.Storage;
using ChatApplication.Utils;
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
        var response = await _httpClient.PostAsync($"/Conversations/conversations/{conversationId}/messages", jsonContent);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"http://localhost/Conversations/conversations/{conversationId}/messages", response.Headers.GetValues("Location").First());
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
        var response = await _httpClient.PostAsync($"/Conversations/conversations/{conversationId}/messages", jsonContent);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _conversationServiceMock.Verify(mock => mock.AddMessage(message), Times.Never);
    }

    [Fact]

    public async Task AddProfile_ConversationNotFound()
    {
        var messageRequest = new MessageRequest("1234", "ronald", "hey bro wanna hit the gym");
        string conversationId = "456";
        _conversationServiceMock.Setup(x => x.AddMessage(
            It.Is<Message>(m =>
                m.messageId == messageRequest.messageId &&
                m.senderUsername == messageRequest.senderUsername &&
                m.messageContent == messageRequest.messageContent &&
                m.conversationId == conversationId
            ))).ThrowsAsync(new ConversationNotFoundException(conversationId));
        var jsonContent = new StringContent(JsonConvert.SerializeObject(messageRequest), Encoding.Default, "application/json");
        var response = await _httpClient.PostAsync($"/Conversations/conversations/{conversationId}/messages", jsonContent);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }


    [Fact]

    public async Task AddMessage_MessageAlreadyExists()
    {
        var messageRequest = new MessageRequest("1234", "ronald", "hey bro wanna hit the gym");
        string conversationId = "456";
        var message = new Message(messageRequest.messageId, messageRequest.senderUsername, messageRequest.messageContent,1000, conversationId);
        _conversationServiceMock.Setup(x => x.AddMessage(
            It.Is<Message>(m =>
                m.messageId == messageRequest.messageId &&
                m.senderUsername == messageRequest.senderUsername &&
                m.messageContent == messageRequest.messageContent &&
                m.conversationId == conversationId
            ))).ThrowsAsync(new MessageAlreadyExistsException(message.messageId));
        var jsonContent = new StringContent(JsonConvert.SerializeObject(messageRequest), Encoding.Default, "application/json");
        var response = await _httpClient.PostAsync($"/Conversations/conversations/{conversationId}/messages", jsonContent);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddMessage_CosmosException()
    {
        var messageRequest = new MessageRequest("1234", "ronald", "hey bro wanna hit the gym");
        string conversationId = "456";
        var message = new Message(messageRequest.messageId, messageRequest.senderUsername, messageRequest.messageContent,1000, conversationId);
        _conversationServiceMock.Setup(x => x.AddMessage(
            It.Is<Message>(m =>
                m.messageId == messageRequest.messageId &&
                m.senderUsername == messageRequest.senderUsername &&
                m.messageContent == messageRequest.messageContent &&
                m.conversationId == conversationId
            ))).ThrowsAsync(new CosmosException("error", HttpStatusCode.BadRequest, 0, "error", 0));
        var jsonContent = new StringContent(JsonConvert.SerializeObject(messageRequest), Encoding.Default, "application/json");
        var response = await _httpClient.PostAsync($"/Conversations/conversations/{conversationId}/messages", jsonContent);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}