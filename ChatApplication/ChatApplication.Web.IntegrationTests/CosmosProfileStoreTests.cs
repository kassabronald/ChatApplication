using ChatApplication.Exceptions;
using ChatApplication.Storage;
using ChatApplication.Web.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApplication.Web.IntegrationTests;

public class CosmosProfileStoreTests:IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{

    private readonly IProfileStore _store;
    private readonly Profile _profile = new(
        Username: Guid.NewGuid().ToString(),
        FirstName: "Foo",
        LastName: "Bar",
        ProfilePictureId: "123"
    );
    
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _store.DeleteProfile(_profile.Username);
    }
    
    public CosmosProfileStoreTests(WebApplicationFactory<Program> factory)
    {
        _store = factory.Services.GetRequiredService<IProfileStore>();
    }
    

    [Fact]

    public async Task AddProfile()
    {
        await _store.AddProfile(_profile);
        Assert.Equal(_profile, await _store.GetProfile(_profile.Username));
    }
    
    [Fact]
    public async Task GetNonExistingProfile()
    {
        await Assert.ThrowsAsync<ProfileNotFoundException>(async () => await _store.GetProfile(_profile.Username + "1"));

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
    public async Task AddProfile_InvalidArgs(string username, string firstName, string lastName,
        string profilePictureId)
    {
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _store.AddProfile(new Profile(username, firstName, lastName, profilePictureId));

        });

    }

    [Fact]

    public async Task AddProfile_NoImage()
    {
        var profile = new Profile(Guid.NewGuid().ToString(), "Foo", "Bar");
        await _store.AddProfile(profile);
        var returnedProfile = await _store.GetProfile(profile.Username);
        await _store.DeleteProfile(profile.Username);
        Assert.Equal(profile, returnedProfile);
        
    }

    [Fact]

    public async Task DeleteProfile()
    {
        await _store.AddProfile(_profile);
        await _store.DeleteProfile(_profile.Username);
        await Assert.ThrowsAsync<ProfileNotFoundException>(async()=> await _store.GetProfile(_profile.Username));
    }

    [Fact]

    public async Task DeleteEmptyProfile()
    {
        await Assert.ThrowsAsync<CosmosException>(async () =>
        {
            await _store.DeleteProfile("");
        });
    }


    [Fact]

    public async Task AddProfileAlreadyExisting()
    {
        await _store.AddProfile(_profile);
        await Assert.ThrowsAsync<ProfileAlreadyExistsException>(async () =>
        {
            await _store.AddProfile(_profile);
        });
    }
    
}