using ChatApplication.Exceptions;
using ChatApplication.Services;
using ChatApplication.Storage;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;
using Moq;

namespace ChatApplication.Web.Tests.Services;

public class ConversationServiceTests
{
    private readonly Mock<IMessageStore> _messageStoreMock = new();
    private readonly Mock<IConversationStore> _conversationStoreMock = new();
    private readonly Mock<IProfileStore> _profileStoreMock = new();
    private readonly ConversationService _conversationService;
    public ConversationServiceTests()
    {
        _conversationService = new ConversationService(_messageStoreMock.Object, _conversationStoreMock.Object, _profileStoreMock.Object);
    }

    [Fact]

    public async Task AddMessage()
    {
        var message = new Message("123", "jad", "bro got W rizz", 1000,"1234");
        var profile = new Profile("jad", "Jad", "Haddad", "12345");
        var participants = new List<Profile> { profile };
        var conversation = new Conversation("1234", participants, 100000);
        _conversationStoreMock.Setup(m => m.GetConversation(message.conversationId)).ReturnsAsync(conversation);
        await _conversationService.AddMessage(message);
        _conversationStoreMock.Verify(mock => mock.GetConversation(message.conversationId), Times.Once);
        _conversationStoreMock.Verify(mock => mock.ChangeConversationLastMessageTime(conversation, message.createdUnixTime), Times.Once);
        _messageStoreMock.Verify(mock => mock.AddMessage(message), Times.Once);
    }


    [Fact]

    public async Task AddMessage_ConversationNotFound()
    {
        var message = new Message("123", "jad", "bro got W rizz",1000, "1234");
        var profile = new Profile("jad", "Jad", "Haddad", "12345");
        //make a List<Profile>  A list not array
        var participants = new List<Profile> { profile };
        var conversation = new Conversation("1234", participants, 100000);
        _conversationStoreMock.Setup(m => m.GetConversation(message.conversationId)).ThrowsAsync(new ConversationNotFoundException());
        await Assert.ThrowsAsync<ConversationNotFoundException>(async () =>
        {
            await _conversationService.AddMessage(message);
        });
        _conversationStoreMock.Verify(mock => mock.GetConversation(message.conversationId), Times.Once);
        _conversationStoreMock.Verify(mock => mock.ChangeConversationLastMessageTime(conversation, message.createdUnixTime), Times.Never);
        _messageStoreMock.Verify(mock => mock.AddMessage(message), Times.Never);
    }

    [Fact]

    public async Task AddMessage_MessageAlreadyExists()
    {
        var message = new Message("123", "jad", "bro got W rizz",1000, "1234");
        var profile = new Profile("jad", "Jad", "Haddad", "12345");
        var participants = new Profile[1] {profile};
        _messageStoreMock.Setup(m => m.AddMessage(message)).ThrowsAsync(new MessageAlreadyExistsException());
        await Assert.ThrowsAsync<MessageAlreadyExistsException>(async () =>
        {
            await _conversationService.AddMessage(message);
        });
        _messageStoreMock.Verify(mock => mock.AddMessage(message), Times.Once);
    }

}