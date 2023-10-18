using System;
using System.Collections.Generic;
using NetCoreServer;
using Newtonsoft.Json.Linq;
using ModdableWebServer.Attributes;
using ModdableWebServer;
using ModdableWebServer.Helper;

namespace OpenPkgRepo.Routes;


class RouteAccount
{

    [HTTP("PUT", "/account")]
    public static bool PutResponse(HttpRequest request, ServerStruct serverStruct)
    {
        HttpResponse response = new(200);

        if (serverStruct.Headers["Username"] == null || serverStruct.Headers["Password"] == null)
        {
            response = new(409);
            response.SetBody(PkgRepo.ErrorResponse[ErrorCode.InvalidCredentials].ToString());
            serverStruct.Response = response;
            serverStruct.SendResponse();
            return true;
        }
        AccountInfo newAccount = new AccountInfo()
        {
            Username = newAccount.Username = serverStruct.Headers["Username"],
            Password = Hash.Create(serverStruct.Headers["Password"]),
            Location = "Earth",
            BirthDate = new Tuple<int, int, int>(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day),
            Bio = ""
        };
        if (AccountHandler.VerifyUsername(newAccount.Username))
        {
            response = new HttpResponse(409);
            response.SetBody(PkgRepo.ErrorResponse[ErrorCode.NameAlreadyTaken].ToString());
            PkgRepo.LogWarning.Print("User tried to make an account that already exists!");
            serverStruct.Response = response;
            serverStruct.SendResponse();
            return true;
        }

        AccountHandler.AddAccount(newAccount);
        response.SetBody("{\n\"Success\" : \"Account has been created\"\n}");
        AccountHandler.LogAccount.Print($"Created account for {newAccount.Username}.");
        serverStruct.Response = response;
        serverStruct.SendResponse();
        return true;
    }





    [HTTP("GET", "/account")]
    public static bool GetResponse(HttpRequest request, ServerStruct serverStruct)
    {
        var response = new HttpResponse(200);
        // Verifies that the username + password combo is valid and works,
        if (serverStruct.Headers["Username"] != null && serverStruct.Headers["Password"] != null)
        {
            if (AccountHandler.VerifyPassword(new AccountInfo() { Username = serverStruct.Headers["Username"], Password = serverStruct.Headers["Password"] }))
            {
                response.SetBody("{\n\"Success\" : \"Username + Password works.\"\n}");
                serverStruct.Response = response;
                serverStruct.SendResponse();
                return true;
            }
            response.SetBody(PkgRepo.ErrorResponse[ErrorCode.InvalidCredentials].ToString());
            serverStruct.Response = response;
            serverStruct.SendResponse();
            return true;
        }
        // returns true if an account with this ID exists.  
        if (serverStruct.Headers["ID"] != null)
        {

        }
        // Returns the ID of the account with this username.
        if (serverStruct.Headers["Username"] != null)
        {
            AccountHandler.LogAccount.Print("Doing query.");
            var results = AccountHandler.GetId(serverStruct.Headers["Username"]);
            if (results == new List<KeyValuePair<int, string>>())
            {
                response.SetBody(PkgRepo.ErrorResponse[ErrorCode.NoItemFound].ToString());
                serverStruct.Response = response;
                serverStruct.SendResponse();
                return true;
            }
            response.SetBody(JArray.FromObject(results).ToString());
            serverStruct.Response = response;
            serverStruct.SendResponse();
            return true;
        }

        response = new HttpResponse(409);

        response.SetBody(PkgRepo.ErrorResponse[ErrorCode.InvalidCredentials].ToString());
        serverStruct.Response = response;
        serverStruct.SendResponse();
        return true;
    }

    [HTTP("DELETE", "/account")]
    public static bool DeleteResponse(HttpRequest request, ServerStruct serverStruct)
    {
        serverStruct.Response.MakeOkResponse();
        serverStruct.SendResponse();
        return true;
    }
}
