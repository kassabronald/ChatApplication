﻿using ChatApplication.Storage;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApplication.Web.IntegrationTests;

public class CosmosProfileStoreTests:IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{

    private readonly IProfileStore _store;
    private readonly Profile _profile = new(
        username: Guid.NewGuid().ToString(),
        firstName: "Foo",
        lastName: "Bar",
        ProfilePictureId: "123"
    );
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
    
    public CosmosProfileStoreTests(WebApplicationFactory<Program> factory)
    {
        _store = factory.Services.GetRequiredService<IProfileStore>();
    }
    

    [Fact]

    public async Task AddProfile()
    {
        await _store.AddProfile(_profile);
        Assert.Equal(_profile, await _store.GetProfile(_profile.username));
    }
    
    [Fact]
    public async Task GetNonExistingProfile()
    {
        var nonExistingProfileUsername = _profile.username + "1";
        Assert.Null(await _store.GetProfile(nonExistingProfileUsername));
    }
    
    [Fact]

    public async Task GetEmptyProfile()
    {
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.GetProfile("");
        });
    }


    [Theory]
    [InlineData(null, "Foo", "Bar", "123")]
    [InlineData("", "Foo", "Bar", "123")]
    [InlineData(" ", "Foo", "Bar", "123")]
    [InlineData("foobar", null, "Bar", "123")]
    [InlineData("foobar", "", "Bar", "123")]
    [InlineData("foobar", " ", "Bar", "123")]
    [InlineData("foobar", "Foo", null, "123")]
    [InlineData("foobar", "Foo", "", "123")]
    [InlineData("foobar", "Foo", " ", "123")]
    [InlineData("foobar", "Foo", "Bar", null)]
    [InlineData("foobar", "Foo", "Bar", "")]
    [InlineData("foobar", "Foo", "Bar", " ")]
    public async Task AddProfile_InvalidArgs(string username, string firstName, string lastName,
        string profilePictureId)
    {
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _store.AddProfile(new Profile(username, firstName, lastName, profilePictureId));

        });

    }
    
}