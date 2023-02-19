using ChatApplication.Storage;
using ChatApplication.Web.Dtos;
using ChatApplication.Web;


namespace ChatApplication.Web.Tests.Storage;

public class InMemoryProfileStoreTests
{
    private readonly InMemoryProfileStore _store = new();
    
    [Fact]
    public async Task AddNewProfile()
    {
        var profile = new Profile(username: "foobar", firstName: "Foo", lastName: "Bar", ProfilePictureId: "123");
        await _store.AddProfile(profile);
        Assert.Equal(profile, await _store.GetProfile(profile.username));
    }
    
    

    [Theory]
    [InlineData(null, "Foo", "Bar", "123")]
    [InlineData("", "Foo", "Bar", "123")]
    [InlineData(" ", "Foo", "Bar", "123")]
    [InlineData("foobar", null, "Bar", "123")]
    [InlineData("foobar", "", "Bar", "123")]
    [InlineData("foobar", "   ", "Bar", "123")]
    [InlineData("foobar", "Foo", "", "123")]
    [InlineData("foobar", "Foo", null, "123")]
    [InlineData("foobar", "Foo", " ", "123")]
    [InlineData("foobar", "Foo", "Bar", null)]
    [InlineData("foobar", "Foo", "Bar", "")]
    [InlineData("foobar", "Foo", "Bar", " ")]
    public async Task UpsertProfile_InvalidArgs(string username, string firstName, string lastName, string profilePictureId)
    {
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _store.AddProfile(new Profile(username, firstName, lastName, profilePictureId));
        });
    }
    
    
    
    
    //get a profile that does not exist
    [Fact]
    public async Task GetNonExistingProfile()
    {
        Assert.Null(await _store.GetProfile("foobar"));
    }
}