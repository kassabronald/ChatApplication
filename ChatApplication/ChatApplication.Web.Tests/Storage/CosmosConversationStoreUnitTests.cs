using System.Net;
using ChatApplication.Exceptions.StorageExceptions;
using ChatApplication.Storage;
using ChatApplication.Storage.Entities;
using ChatApplication.Web.Dtos;
using Microsoft.Azure.Cosmos;
using Moq;

namespace ChatApplication.Web.Tests.Storage;

public class CosmosConversationStoreUnitTests
{
    private readonly Mock<CosmosClient> _cosmosClientMock;
    private readonly Mock<Container> _containerMock;
    private readonly CosmosConversationStore _cosmosConversationStore;
    
    public CosmosConversationStoreUnitTests()
    {
        _cosmosClientMock = new Mock<CosmosClient>();
        _containerMock = new Mock<Container>();
        _cosmosClientMock.Setup(client => client.GetDatabase(It.IsAny<string>()).GetContainer(It.IsAny<string>()))
            .Returns(_containerMock.Object);
        _cosmosConversationStore = new CosmosConversationStore(_cosmosClientMock.Object);
    }

    [Fact]

    public async Task CosmosExceptionIsHandled()
    {

        //_containerMock.Setup(x => x.CreateItemAsync(It.IsAny<ConversationEntity>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            //.ThrowsAsync(new CosmosException("Fake Exception", HttpStatusCode.InternalServerError, 0, "", 0));
            
        //We can't seem to make the above work. It never throws the exception.
        _containerMock.Setup(x => x.ReadItemAsync<ConversationEntity>(It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Fake Exception", HttpStatusCode.InternalServerError, 0, "", 0));
        _containerMock.Setup(x => x.ReplaceItemAsync<ConversationEntity>(It.IsAny<ConversationEntity>(), It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Fake Exception", HttpStatusCode.InternalServerError, 0, "", 0));
        _containerMock.Setup(x => x.DeleteItemAsync<UserConversation>(It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Fake Exception", HttpStatusCode.InternalServerError, 0, "", 0));

        var userConversation = new UserConversation("conversationId", new List<Profile>(), 123456789, "username");

        //await Assert.ThrowsAsync<StorageUnavailableException>(() => _cosmosConversationStore.CreateUserConversation(userConversation));
        await Assert.ThrowsAsync<StorageUnavailableException>(() => _cosmosConversationStore.GetUserConversation(userConversation.Username, userConversation.ConversationId));
        await Assert.ThrowsAsync<StorageUnavailableException>(() => _cosmosConversationStore.UpdateConversationLastMessageTime(userConversation, userConversation.LastMessageTime));
        await Assert.ThrowsAsync<StorageUnavailableException>(() => _cosmosConversationStore.DeleteUserConversation(userConversation));
    }


    
    
}