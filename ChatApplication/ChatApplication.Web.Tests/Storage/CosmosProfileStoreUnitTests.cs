using System.Net;
using ChatApplication.Exceptions.StorageExceptions;
using ChatApplication.Storage;
using ChatApplication.Storage.Entities;
using ChatApplication.Web.Dtos;
using Microsoft.Azure.Cosmos;
using Moq;

namespace ChatApplication.Web.Tests.Storage;

public class CosmosProfileStoreUnitTests
{
    private readonly Mock<CosmosClient> _cosmosClientMock;
    private readonly Mock<Container> _containerMock;
    private readonly CosmosProfileStore _cosmosProfileStore;
    
    public CosmosProfileStoreUnitTests()
    {
        _cosmosClientMock = new Mock<CosmosClient>();
        _containerMock = new Mock<Container>();
        _cosmosClientMock.Setup(client => client.GetDatabase(It.IsAny<string>()).GetContainer(It.IsAny<string>()))
            .Returns(_containerMock.Object);
        _cosmosProfileStore = new CosmosProfileStore(_cosmosClientMock.Object);
    }
    
    [Fact]
    public async Task CosmosExceptionIsHandled()
    {
        //_containerMock.Setup(x => x.CreateItemAsync(It.IsAny<ProfileEntity>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            //.ThrowsAsync(new CosmosException("Fake Exception", HttpStatusCode.InternalServerError, 0, "", 0));
        _containerMock.Setup(x => x.ReadItemAsync<ProfileEntity>(It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Fake Exception", HttpStatusCode.InternalServerError, 0, "", 0));
        _containerMock.Setup(x => x.DeleteItemAsync<Profile>(It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Fake Exception", HttpStatusCode.InternalServerError, 0, "", 0));

        var profile = new Profile("username", "firstName", "lastName", "profilePictureId");

        //await Assert.ThrowsAsync<StorageUnavailableException>(() => _cosmosProfileStore.AddProfile(profile));
        await Assert.ThrowsAsync<StorageUnavailableException>(() => _cosmosProfileStore.GetProfile(profile.Username));
        await Assert.ThrowsAsync<StorageUnavailableException>(() => _cosmosProfileStore.DeleteProfile(profile.Username));
    }
}