using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ChatApplication.Exceptions.StorageExceptions;
using ChatApplication.Storage;
using Moq;
using Moq.Protected;

namespace ChatApplication.Web.Tests.Storage;

public class BlobImageStoreTests
{
    private readonly Mock<BlobContainerClient> _blobContainerClientMock;
    private readonly BlobImageStore _blobImageStore;

    public BlobImageStoreTests()
    {
        _blobContainerClientMock = new Mock<BlobContainerClient>();
        _blobImageStore = new BlobImageStore(_blobContainerClientMock.Object);
    }

    [Fact]
    public async Task RequestFailedExceptionIsHandled()
    {
        const string blobName = "testBlob";
        var contentType = "image/jpeg";
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("Test data"));

        var blobClientMock = new Mock<BlobClient>();
        
        _blobContainerClientMock.Setup(x => x.GetBlobClient(blobName)).Returns(blobClientMock.Object);

        blobClientMock.Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(500, "Fake RequestFailedException"));
        blobClientMock.Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(500, "Fake RequestFailedException"));
        blobClientMock
            .Setup(x => x.DownloadAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(500, "Fake RequestFailedException"));
        blobClientMock
            .Setup(x => x.DeleteAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(500, "Fake RequestFailedException"));



        //await Assert.ThrowsAsync<StorageUnavailableException>(() => _blobImageStore.AddImage(blobName, memoryStream, contentType));
        await Assert.ThrowsAsync<StorageUnavailableException>(() => _blobImageStore.GetImage(blobName));
        await Assert.ThrowsAsync<StorageUnavailableException>(() => _blobImageStore.DeleteImage(blobName));
    }

}
