﻿using System.Net;
using ChatApplication.Exceptions;
using ChatApplication.Exceptions.StorageExceptions;
using ChatApplication.Services;
using ChatApplication.Utils;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace ChatApplication.Web.Tests.Controllers;

public class ImagesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly Mock<IImageService> _imageServiceMock = new();
    private readonly HttpClient _httpClient;
    
    public ImagesControllerTests(WebApplicationFactory<Program> factory)
    {
        
        _httpClient = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services => { services.AddSingleton(_imageServiceMock.Object); });
        }).CreateClient();
    }

    
    

    [Fact]

    public async Task GetImage_Success_200()
    {
        var image = new byte[] {1, 2, 3, 4, 5};
        var imageId = "123";
        
        Image expectedImage = new(image, "image/jpeg");
        _imageServiceMock.Setup(m => m.GetImage(imageId)).ReturnsAsync(expectedImage);
        
        var response = await _httpClient.GetAsync($"/api/Images/{imageId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var contentActual = await response.Content.ReadAsByteArrayAsync();
        var contentTypeActual = response.Content.Headers.ContentType?.ToString();
        
        Assert.Equal(expectedImage.ImageData, contentActual);
        Assert.Equal(expectedImage.ContentType, contentTypeActual);
        
    }

    [Fact]
    public async Task GetImage_NotFound_404()
    {
        _imageServiceMock.Setup(m => m.GetImage("123")).ThrowsAsync(new ImageNotFoundException("Image not Found"));
        var response = await _httpClient.GetAsync($"/api/Images/123");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    
    public async Task GetImage_InvalidId_400()
    {
        _imageServiceMock.Setup(m => m.GetImage("123")).ThrowsAsync(new ArgumentException());
        var response = await _httpClient.GetAsync($"/api/Images/123");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
    }

    [Fact]

    public async Task GetImage_StorageUnavailable_503()
    {
        _imageServiceMock.Setup(m => m.GetImage("123"))
            .ThrowsAsync(new StorageUnavailableException("database is down"));
        var response = await _httpClient.GetAsync($"/api/Images/123");
        
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
    
    [Fact]
    public async Task UploadImage_Success_201()
    {
        var image = new byte[] { 1, 2, 3, 4, 5 };
        const string imageId = "123";
        const string fileName = "test.jpeg";
        var streamFile = new MemoryStream(image);
        IFormFile file = new FormFile(streamFile, 0, streamFile.Length, "id_from_form", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };
        var uploadRequest = new UploadImageRequest(file);
        _imageServiceMock.Setup(m => m.AddImage(It.IsAny<MemoryStream>(), "image/jpeg")).ReturnsAsync(imageId);
        
        using var formData = new MultipartFormDataContent();
        var requestContent = new StreamContent(uploadRequest.File.OpenReadStream());
        requestContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        formData.Add(requestContent, "File", uploadRequest.File.FileName);
        
        var response = await _httpClient.PostAsync("/api/Images", formData);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        _imageServiceMock.Verify(mock => mock.AddImage(It.IsAny<MemoryStream>(), "image/jpeg"), Times.Once);
    }
    
    [Fact]
    
    public async Task UploadImage_PDFContentType_201()
    {
        var image = new byte[] { 1, 2, 3, 4, 5 };
        const string fileName = "test.pdf";
        const string imageId = "1";
        var streamFile = new MemoryStream(image);
        IFormFile file = new FormFile(streamFile, 0, streamFile.Length, "id_from_form", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/pdf"
        };
        var uploadRequest = new UploadImageRequest(file);
        _imageServiceMock.Setup(m => m.AddImage(It.IsAny<MemoryStream>(), It.IsAny<String>())).ReturnsAsync(imageId);
        
        using var formData = new MultipartFormDataContent();
        formData.Add(new StreamContent(uploadRequest.File.OpenReadStream()), "File", uploadRequest.File.FileName);
        
        var response = await _httpClient.PostAsync("/api/images", formData);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        _imageServiceMock.Verify(mock => mock.AddImage(It.IsAny<MemoryStream>(), It.IsAny<String>()), Times.Once);
    }
    
    
    [Fact]
    
    public async Task UploadImage_EmptyFile_200()
    {
        var image = Array.Empty<byte>();
        const string fileName = "test.jpg";
        var streamFile = new MemoryStream(image);
        IFormFile file = new FormFile(streamFile, 0, streamFile.Length, "id_from_form", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpg"
        };
        var uploadRequest = new UploadImageRequest(file);
        _imageServiceMock.Setup(m => m.AddImage(It.IsAny<MemoryStream>(), It.IsAny<String>()));
        
        using var formData = new MultipartFormDataContent();
        formData.Add(new StreamContent(uploadRequest.File.OpenReadStream()), "File", uploadRequest.File.FileName);
        
        var response = await _httpClient.PostAsync("/api/Images", formData);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _imageServiceMock.Verify(mock => mock.AddImage(It.IsAny<MemoryStream>(), It.IsAny<String>()), Times.Never);
    }

    [Fact]

    public async Task UploadImage_StorageUnavailable_503()
    {
        var image = new byte[] { 1, 2, 3, 4, 5 };
        const string fileName = "test.pdf";
        const string imageId = "1";
        var streamFile = new MemoryStream(image);
        IFormFile file = new FormFile(streamFile, 0, streamFile.Length, "id_from_form", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/pdf"
        };
        var uploadRequest = new UploadImageRequest(file);
        _imageServiceMock.Setup(m => m.AddImage(It.IsAny<MemoryStream>(), It.IsAny<String>()))
            .ThrowsAsync(new StorageUnavailableException("database is down"));
        
        using var formData = new MultipartFormDataContent();
        formData.Add(new StreamContent(uploadRequest.File.OpenReadStream()), "File", uploadRequest.File.FileName);
        
        var response = await _httpClient.PostAsync("/api/images", formData);
        
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
    
}

