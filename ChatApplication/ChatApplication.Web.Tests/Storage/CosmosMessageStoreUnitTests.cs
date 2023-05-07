using System.Net;
using ChatApplication.Exceptions.StorageExceptions;
using ChatApplication.Storage;
using ChatApplication.Storage.Entities;
using ChatApplication.Web.Dtos;
using Microsoft.Azure.Cosmos;
using Moq;

namespace ChatApplication.Web.Tests.Storage;

public class CosmosMessageStoreUnitTests
{
    private readonly Mock<CosmosClient> _cosmosClientMock;
    private readonly Mock<Container> _containerMock;
    private readonly CosmosMessageStore _cosmosMessageStore;
    
    public CosmosMessageStoreUnitTests()
    {
        _cosmosClientMock = new Mock<CosmosClient>();
        _containerMock = new Mock<Container>();
        _cosmosClientMock.Setup(client => client.GetDatabase(It.IsAny<string>()).GetContainer(It.IsAny<string>()))
            .Returns(_containerMock.Object);
        _cosmosMessageStore = new CosmosMessageStore(_cosmosClientMock.Object);
    }
    
    [Fact]
    public async Task CosmosExceptionIsHandled()
    {
        //_containerMock.Setup(x => x.CreateItemAsync(It.IsAny<MessageEntity>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            //.ThrowsAsync(new CosmosException("Fake Exception", HttpStatusCode.InternalServerError, 0, "", 0));
        _containerMock.Setup(x => x.ReadItemAsync<MessageEntity>(It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Fake Exception", HttpStatusCode.InternalServerError, 0, "", 0));
        _containerMock.Setup(x => x.DeleteItemAsync<Message>(It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Fake Exception", HttpStatusCode.InternalServerError, 0, "", 0));

        var message = new Message("messageId", "senderUsername", "text",  "conversationId", 123456789);

        //await Assert.ThrowsAsync<StorageUnavailableException>(() => _cosmosMessageStore.AddMessage(message));
        await Assert.ThrowsAsync<StorageUnavailableException>(() => _cosmosMessageStore.GetMessage(message.ConversationId, message.MessageId));
        await Assert.ThrowsAsync<StorageUnavailableException>(() => _cosmosMessageStore.DeleteMessage(message));
    }
}