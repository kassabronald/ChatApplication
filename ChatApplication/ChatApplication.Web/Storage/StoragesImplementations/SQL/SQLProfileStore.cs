using ChatApplication.Exceptions;
using ChatApplication.Web.Dtos;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ChatApplication.Storage.SQL;

public class SQLProfileStore : IProfileStore
{

    private readonly SqlConnection _sqlConnection;


    public SQLProfileStore(SqlConnection sqlConnection)
    {
        _sqlConnection = sqlConnection;

    }

    public async Task AddProfile(Profile profile)
    {
        var query = @"INSERT INTO Profiles (Username, FirstName, LastName, ProfilePictureId)
              VALUES (@Username, @FirstName, @LastName, @ProfilePictureId)";

        try
        {
            await _sqlConnection.ExecuteAsync(query, profile);
        }

        catch (SqlException ex)
        {
            if (ex.Number == 2627) 
            {
                throw new ProfileAlreadyExistsException($"Profile with username {profile.Username} already exists");
            }
            else
            {
                throw;
            }
        }
    }


    public async Task<Profile> GetProfile(string username)
    {
        var query = "SELECT * FROM Profiles WHERE Username = @Username";
        var profile = await _sqlConnection.QueryFirstOrDefaultAsync<Profile>(query, new { Username = username });

        if (profile == null)
        {
            throw new ProfileNotFoundException($"A profile with username {username} was not found");
        }

        return profile;
    }

    public async Task DeleteProfile(string username)
    {
        var query = "DELETE FROM Profiles WHERE Username = @Username";

        await _sqlConnection.ExecuteAsync(query, new { Username = username });
    }
}