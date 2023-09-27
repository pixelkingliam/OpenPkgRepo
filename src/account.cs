using System;
using System.Collections.Generic;
using System.Linq;
//using System.Security.Cryptography;
using System.Threading.Tasks;
using NetCoreServer;
using Newtonsoft.Json.Linq;
using Pixel.OakLog;


namespace OpenPkgRepo;

public struct AccountInfo
{
    public string Username;
    public string Password;
    public string Location;
    public Tuple<int, int, int> BirthDate;
    public string Bio;
    // pages array
    // package array, maybe.
}
static class AccountHandler
{
    public static readonly OLog LogAccount = OLog.Create("ACCOUNT", (102, 205, 170));

    // Routes

    // Functions for routes.
    public static bool VerifyUsername(string username)
    {
        using var command = PkgRepo.Database.CreateCommand();
        command.CommandText =
            @"
                SELECT Username
                FROM Accounts
                WHERE Username = $Username
                ";
        command.Parameters.AddWithValue("$Username", username);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if ((string)reader.GetValue(0) == username)
            {
                return true;
            }
        }
        return false;
    }

    public static List<KeyValuePair<int, string>> GetId(string username)
    {
        using var command = PkgRepo.Database.CreateCommand();
        List<KeyValuePair<int, string>> returnDict = new();
        command.CommandText =
            """

                                SELECT *
                                FROM Accounts
                                WHERE Username LIKE $Username
                
                """;
        command.Parameters.AddWithValue("$Username", $"%{username}%");
        using var reader = command.ExecuteReader();
        if (!reader.HasRows)
        {
            return new List<KeyValuePair<int, string>>();
        }
        while (reader.Read())
        {
            if (reader.IsDBNull(0))
            {
                continue;
            }
            returnDict.Add(new KeyValuePair<int, string>(reader.GetInt32(0), reader.GetString(1)));
        }
        return returnDict;
    }

    public static bool VerifyPassword(AccountInfo accountInfo)
    {
        using var command = PkgRepo.Database.CreateCommand();
        command.CommandText =
            @"
                SELECT Password
                FROM Accounts
                WHERE Username = $Username
                ";
        command.Parameters.AddWithValue("$Username", accountInfo.Username);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (Hash.Verify(accountInfo.Password, reader.GetString(0)))
            {
                return true;
            }
        }
        return false;
    }
    private static bool VerifyId(int id)
    {
        using var command = PkgRepo.Database.CreateCommand();
        command.CommandText =
            """

                                SELECT AccountsID
                                FROM Accounts
                                WHERE AccountsID = $ID
                
                """;
        command.Parameters.AddWithValue("$ID", id);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {

            if (reader.GetInt32(0) == id)
            {
                return true;
            }
        }
        return false;
    }
    public static async void AddAccount(AccountInfo newAccount)
    {
        await using var command = PkgRepo.Database.CreateCommand();
        command.CommandText =
            """

                                INSERT INTO Accounts (Username, Password, Location, BirthDate, Bio)
                                VALUES ($Username, $Password, "", "", "")

                """;
        command.Parameters.AddWithValue("$Username", newAccount.Username);
        command.Parameters.AddWithValue("$Password", newAccount.Password);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DeleteAccount(AccountInfo accountInfo)
    {
        await using var command = PkgRepo.Database.CreateCommand();
        command.CommandText =
            """

                                DELETE FROM Accounts
                                WHERE Username = $Username AND Password = $Password
                
                """;
        command.Parameters.AddWithValue("$Username", accountInfo.Username);
        command.Parameters.AddWithValue("$Password", accountInfo.Password);
        await command.ExecuteNonQueryAsync();
    }
    private static async Task<String> GetNameFromId(int id)
    {
        await using var command = PkgRepo.Database.CreateCommand();
        command.CommandText =
            """

                                SELECT Username
                                FROM Accounts
                                WHERE AccountsID = $ID
                
                """;
        command.Parameters.AddWithValue("$ID", id);
        await using var reader = await command.ExecuteReaderAsync();
        while (reader.Read())
        {

            return reader.GetString(0);

        }
        return string.Empty;
    }
    public static AccountInfo GetAccountFromId(int id)
    {
        using var command = PkgRepo.Database.CreateCommand();
        command.CommandText = @"
        SELECT *
        FROM Accounts
        WHERE AccountsID = $ID";
        command.Parameters.AddWithValue("$ID", id);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var account = new AccountInfo
            {
                Username = reader.GetString(1),
                Location = reader.GetString(3),
                Bio = reader.GetString(5)
            };
            var rawBirthDate = reader.GetString(4);
            account.BirthDate = rawBirthDate == "" ?
                new Tuple<int, int, int>(0, 0, 0) :
                new Tuple<int, int, int>(Convert.ToInt32(rawBirthDate.Split('.')[0]), Convert.ToInt32(rawBirthDate.Split('.')[1]), Convert.ToInt32(rawBirthDate.Split('.')[2]));
            return account;
        }
        return new AccountInfo();
    }
    public static async Task<AccountInfo> GetAccountFromIdAsync(int id)
    {
        await using var command = PkgRepo.Database.CreateCommand();
        command.CommandText =
            """
                
                                SELECT *
                                FROM Accounts
                                WHERE AccountsID = $ID

                """;
        command.Parameters.AddWithValue("$ID", id);
        await using var reader = await command.ExecuteReaderAsync();
        while (reader.Read())
        {
            var account = new AccountInfo
            {
                Username = reader.GetString(1),
                Location = reader.GetString(3),
                Bio = reader.GetString(5)
            };
            var rawBirthDate = reader.GetString(4);
            account.BirthDate = new Tuple<int, int, int>(Convert.ToInt32(rawBirthDate.Split('.')[0]), Convert.ToInt32(rawBirthDate.Split('.')[1]), Convert.ToInt32(rawBirthDate.Split('.')[2]));
            return account;
        }
        return new AccountInfo();
    }
}
