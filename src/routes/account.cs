using System;
using System.Collections.Generic;
using System.Linq;
//using System.Security.Cryptography;
using System.Threading.Tasks;
using NetCoreServer;
using Newtonsoft.Json.Linq;
using OakLogger;

namespace OpenPkgRepo
{
    public struct AccountInfo
    {
        public string Username;
        public string Password;
        public string Location;
        public Tuple<int,int,int> BirthDate;
        public string Bio;
        // pages array
        // package array, maybe.
    }
    static class AccountHandler
    {
        public static readonly OLog LogAccount = PkgRepo.BuildLogger("ACCOUNT", new Tuple<byte, byte, byte>(102, 205, 170));

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
                    new Tuple<int, int, int>(0,0,0) :
                    new Tuple<int, int, int>(Convert.ToInt32(rawBirthDate.Split('.')[0]),Convert.ToInt32(rawBirthDate.Split('.')[1]),Convert.ToInt32(rawBirthDate.Split('.')[2]));
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
                account.BirthDate = new Tuple<int, int, int>(Convert.ToInt32(rawBirthDate.Split('.')[0]),Convert.ToInt32(rawBirthDate.Split('.')[1]),Convert.ToInt32(rawBirthDate.Split('.')[2]));
                return account;
            }
            return new AccountInfo();
        }
    }
}

namespace OpenPkgRepo.Routes
{
    [RouteClass]
    class RouteAccount : StaticRoute
    {
        public override string RouteUrl => "/account";

        public override HttpResponse  PutResponse(HttpContext ctx)
        {
            HttpResponse response = new(200);

            if (ctx.Headers["Username"] == null || ctx.Headers["Password"] == null)
            {
                response = new(409);
                response.SetBody(PkgRepo.ErrorResponse[ErrorCode.InvalidCredentials].ToString());
                return response;
            }
            AccountInfo newAccount = new AccountInfo()
                {
                    Username = newAccount.Username = ctx.Headers["Username"],
                    Password = Hash.Create(ctx.Headers["Password"]),
                    Location = "Earth",
                    BirthDate = new Tuple<int, int, int>(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day),
                    Bio = ""
                };
            if (AccountHandler.VerifyUsername(newAccount.Username))
            {
                response = new HttpResponse(409);
                response.SetBody(PkgRepo.ErrorResponse[ErrorCode.NameAlreadyTaken].ToString());
                PkgRepo.LogWarning.Print("User tried to make an account that already exists!");
                return response;
            }
            
            AccountHandler.AddAccount(newAccount);
            response.SetBody("{\n\"Success\" : \"Account has been created\"\n}");
            AccountHandler.LogAccount.Print($"Created account for {newAccount.Username}.");
            return response;
        }
        public override HttpResponse  GetResponse(HttpContext ctx)
        {
            var response  = new HttpResponse(200);
            // Verifies that the username + password combo is valid and works,
            if (ctx.Headers["Username"] != null && ctx.Headers["Password"] != null)
            {
                if (AccountHandler.VerifyPassword(new AccountInfo() { Username = ctx.Headers["Username"], Password = ctx.Headers["Password"] }))
                {
                    response.SetBody("{\n\"Success\" : \"Username + Password works.\"\n}");
                    return response;
                }
                response.SetBody(PkgRepo.ErrorResponse[ErrorCode.InvalidCredentials].ToString());
                return response;
            }
            // returns true if an account with this ID exists.  
            if (ctx.Headers["ID"] != null)
            {
                
            }
            // Returns the ID of the account with this username.
            if (ctx.Headers["Username"] != null)
            {
                AccountHandler.LogAccount.Print("Doing query.");
                var results = AccountHandler.GetId(ctx.Headers["Username"]);
                if(results == new List<KeyValuePair<int, string>>())
                {
                    response.SetBody(PkgRepo.ErrorResponse[ErrorCode.NoItemFound].ToString());
                    return response;
                }
                response.SetBody(JArray.FromObject(results).ToString());
                return response;
            }

            response = new HttpResponse(409);

            response.SetBody(PkgRepo.ErrorResponse[ErrorCode.InvalidCredentials].ToString());
            return response;
        }
        
        public override HttpResponse DeleteResponse(HttpContext ctx)
        {
            return new HttpResponse();
        }
    }
    [RouteClass]
    class RouteSession : StaticRoute
    {
        public override string RouteUrl => "/session";
        public override HttpResponse GetResponse(HttpContext ctx)
        {
            var response  = new HttpResponse(200);
            if (ctx.Headers["Username"] != null && ctx.Headers["Password"] != null)
            {
                if (AccountHandler.VerifyPassword(new AccountInfo()
                        { Username = ctx.Headers["Username"], Password = ctx.Headers["Password"] }))
                {

                    var session = new Session(32, DateTime.Now.AddHours((double)PkgRepo.Configuration["SessionExpirationDate"]), AccountHandler.GetId(ctx.Headers["Username"]).SingleOrDefault(a => a.Value == ctx.Headers["Username"]).Key);
                    PkgRepo.Sessions.Add(session);
                    response.SetBody(new JObject(){{"SessionID", session.Id}}.ToString());
                    return response;
                }
            }
            response = new HttpResponse(409);

            response.SetBody(PkgRepo.ErrorResponse[ErrorCode.InvalidCredentials].ToString());
            return response;
        }
    }
    [RouteClass]
    class RouteSessionVerify: StaticRoute
    {
        public override string RouteUrl => "/session/verify";
        public override HttpResponse GetResponse(HttpContext ctx)
        {
            var response  = new HttpResponse(200);
            if (ctx.Headers["Session"] != null)
            {
                
                if(PkgRepo.Sessions.All(i => i.Id != ctx.Headers["Session"]))
                {
                    response = new HttpResponse(409);
                    response.SetBody(PkgRepo.ErrorResponse[ErrorCode.InvalidSession].ToString());
                    return response;
                }
                var session = PkgRepo.Sessions.First(i => i.Id == ctx.Headers["Session"]);
                if (session.HasExpired())
                {
                    response = new HttpResponse(409);
                    response.SetBody(PkgRepo.ErrorResponse[ErrorCode.ExpiredSession].ToString());
                    return response;
                }
                response.SetBody(new JObject()
                {
                    {"SessionID", session.Id},

                    {"WillExpireIn", (session.ExpiryDate.Ticks - DateTime.Now.Ticks)/36000000000d},
                    {"SessionOwner", session.OwnerID}
                }.ToString());
                return response;
                
            }
            response = new HttpResponse(409);

            response.SetBody(PkgRepo.ErrorResponse[ErrorCode.InvalidCredentials].ToString());
            return response;
        }
    }
    
}
