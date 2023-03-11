using System.Net;
using ChatApplication.Exceptions;
using ChatApplication.Storage.Entities;
using ChatApplication.Web.Dtos;
using Microsoft.Azure.Cosmos;


namespace ChatApplication.Storage;

public class CosmosProfileStore : IProfileStore
{
    private readonly CosmosClient _cosmosClient;
    
    
    public CosmosProfileStore(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;
    }
    private Container Container => _cosmosClient.GetDatabase("MainDatabase").GetContainer("Profiles");
    
    public async Task AddProfile(Profile profile)
    {
        if (profile == null ||
            string.IsNullOrWhiteSpace(profile.username) ||
            string.IsNullOrWhiteSpace(profile.firstName) ||
            string.IsNullOrWhiteSpace(profile.lastName) ||
            string.IsNullOrWhiteSpace(profile.ProfilePictureId)
           )
        {
            throw new ArgumentException($"Invalid profile {profile}", nameof(profile));
        }
        var entity = ToEntity(profile);
        try
        {
            await Container.CreateItemAsync(entity);
        }
        catch (Exception e)
        {
            if (e is CosmosException cosmosException && cosmosException.StatusCode == HttpStatusCode.Conflict)
            {
                throw new ProfileAlreadyExistsException($"Profile with username {profile.username} already exists", nameof(profile));
            }
            throw;
        }
    }

    public async Task<Profile?> GetProfile(string username)
    {
       
        try
        {
            var entity = await Container.ReadItemAsync<ProfileEntity>(
                id: username,
                partitionKey: new PartitionKey(username),
                new ItemRequestOptions
                {
                    ConsistencyLevel = ConsistencyLevel.Session 
                }
            );
            return ToProfile(entity);
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            throw;
        }
    }
    
    public async Task DeleteProfile(string username)
    {
        try
        {
            await Container.DeleteItemAsync<Profile>(
                id: username,
                partitionKey: new PartitionKey(username)
            );
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                return;
            }

            throw;
        }
    }
    private ProfileEntity ToEntity(Profile profile)
    {
        return new ProfileEntity(
            partitionKey: profile.username,
            id: profile.username,
            profile.firstName,
            profile.lastName,
            profile.ProfilePictureId
        );
    }
    
    private Profile ToProfile(ProfileEntity entity)
    {
        return new Profile(
            entity.id,
            entity.firstName,
            entity.lastName,
            entity.ProfilePictureId
        );
    }
}