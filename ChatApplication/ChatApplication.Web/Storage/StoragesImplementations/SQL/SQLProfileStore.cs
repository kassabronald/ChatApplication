using ChatApplication.Configuration;
using ChatApplication.Exceptions;
using ChatApplication.Web.Dtos;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace ChatApplication.Storage.SQL;

public class SQLProfileStore : IProfileStore
{

    private readonly string _connectionString;


    public SQLProfileStore(IOptions<SQLSettings> sqlSettings)
    {
        _connectionString = sqlSettings.Value.ConnectionString;
    }
    
    private SqlConnection GetSqlConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public async Task AddProfile(Profile profile)
    {
        var query = @"INSERT INTO Profiles (Username, FirstName, LastName, ProfilePictureId)
              VALUES (TRIM(@Username), TRIM(@FirstName), TRIM(@LastName), TRIM(@ProfilePictureId))";
        
        await using var sqlConnection = GetSqlConnection();
        await sqlConnection.OpenAsync();

        try
        {
            await sqlConnection.ExecuteAsync(query, profile);
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
        
        await sqlConnection.CloseAsync();
    }


    public async Task<Profile> GetProfile(string username)
    {
        var query = "SELECT * FROM Profiles WHERE Username = @Username";
        
        await using var sqlConnection = GetSqlConnection();
        await sqlConnection.OpenAsync();
        
        var profile = await sqlConnection.QueryFirstOrDefaultAsync<Profile>(query, new { Username = username });

        if (profile == null)
        {
            throw new ProfileNotFoundException($"A profile with username {username} was not found");
        }
        
        await sqlConnection.CloseAsync();
        return profile;
    }

    public async Task DeleteProfile(string username)
    {
        var query = "DELETE FROM Profiles WHERE Username = @Username";
        
        await using var sqlConnection = GetSqlConnection();
        await sqlConnection.OpenAsync();
        
        await sqlConnection.ExecuteAsync(query, new { Username = username });
        
        await sqlConnection.CloseAsync();
        
    }
}