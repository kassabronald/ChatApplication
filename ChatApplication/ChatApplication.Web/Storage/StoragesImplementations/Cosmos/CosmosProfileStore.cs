using System.Net;
using ChatApplication.Exceptions;
using ChatApplication.Exceptions.StorageExceptions;
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
            string.IsNullOrWhiteSpace(profile.Username) ||
            string.IsNullOrWhiteSpace(profile.FirstName) ||
            string.IsNullOrWhiteSpace(profile.LastName)
           )
        {
            throw new ArgumentException($"Invalid profile {profile}", nameof(profile));
        }

        var entity = ToEntity(profile);
        try
        {
            await Container.CreateItemAsync(entity);
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.Conflict)
            {
                throw new ProfileAlreadyExistsException($"Profile with username {profile.Username} already exists");
            }
            
            if(e.StatusCode != HttpStatusCode.BadRequest)
            {
                throw new StorageUnavailableException($"Could not add profile with username {profile.Username}", e);
            }

            throw;
        }
    }

    public async Task<Profile> GetProfile(string username)
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
                throw new ProfileNotFoundException($"Profile with username {username} does not exists");
            }

            if(e.StatusCode != HttpStatusCode.BadRequest)
            {
                throw new StorageUnavailableException($"Could not get profile with username {username}", e);
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

            if(e.StatusCode != HttpStatusCode.BadRequest)
            {
                throw new StorageUnavailableException($"Could not delete profile with username {username}", e);
            }

            throw;
        }
    }

    private static ProfileEntity ToEntity(Profile profile)
    {
        return new ProfileEntity(
            partitionKey: profile.Username,
            id: profile.Username,
            profile.FirstName,
            profile.LastName,
            profile.ProfilePictureId
        );
    }

    private static Profile ToProfile(ProfileEntity entity)
    {
        return new Profile(
            entity.id,
            entity.firstName,
            entity.lastName,
            entity.ProfilePictureId
        );
    }
}