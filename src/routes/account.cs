using System;
using System.Collections.Generic;
using System.Linq;
//using System.Security.Cryptography;
using System.Threading.Tasks;
using NetCoreServer;
using Newtonsoft.Json.Linq;
using Pixel.OakLog;

namespace OpenPkgRepo.Routes;

[RouteClass]
class RouteAccount : StaticRoute
{
    public override string RouteUrl => "/account";

    public override HttpResponse PutResponse(HttpContext ctx)
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
    public override HttpResponse GetResponse(HttpContext ctx)
    {
        var response = new HttpResponse(200);
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
            if (results == new List<KeyValuePair<int, string>>())
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
        var response = new HttpResponse(200);
        if (ctx.Headers["Username"] != null && ctx.Headers["Password"] != null)
        {
            if (AccountHandler.VerifyPassword(new AccountInfo()
            { Username = ctx.Headers["Username"], Password = ctx.Headers["Password"] }))
            {

                var session = new Session(32, DateTime.Now.AddHours((double)PkgRepo.Configuration["SessionExpirationDate"]), AccountHandler.GetId(ctx.Headers["Username"]).SingleOrDefault(a => a.Value == ctx.Headers["Username"]).Key);
                PkgRepo.Sessions.Add(session);
                response.SetBody(new JObject() { { "SessionID", session.Id } }.ToString());
                return response;
            }
        }
        response = new HttpResponse(409);

        response.SetBody(PkgRepo.ErrorResponse[ErrorCode.InvalidCredentials].ToString());
        return response;
    }
}
[RouteClass]
class RouteSessionVerify : StaticRoute
{
    public override string RouteUrl => "/session/verify";
    public override HttpResponse GetResponse(HttpContext ctx)
    {
        var response = new HttpResponse(200);
        if (ctx.Headers["Session"] != null)
        {

            if (PkgRepo.Sessions.All(i => i.Id != ctx.Headers["Session"]))
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


