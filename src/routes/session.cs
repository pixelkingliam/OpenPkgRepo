using System;
using System.Linq;
using NetCoreServer;
using Newtonsoft.Json.Linq;
using ModdableWebServer.Attributes;
using ModdableWebServer;
using ModdableWebServer.Helper;

namespace OpenPkgRepo.Routes;


internal class SessionRoute
{

    [HTTP("GET", "/session")]
    public static bool GetSession(HttpRequest request, ServerStruct serverStruct)
    {
        var response = new HttpResponse(200);
        string username = serverStruct.Headers["Username"];
        string password = serverStruct.Headers["Password"];
        if (username != null && password != null)
        {
            if (AccountHandler.VerifyPassword(new AccountInfo()
            { Username = username, Password = password }))
            {

                var session = new Session(32, DateTime.Now.AddHours((double)PkgRepo.Configuration["SessionExpirationDate"]), AccountHandler.GetId(username).SingleOrDefault(a => a.Value == username).Key);
                PkgRepo.Sessions.Add(session);
                response.SetBody(new JObject() { { "SessionID", session.Id } }.ToString());
                serverStruct.Response = response;
                serverStruct.SendResponse();
                return true;
            }
        }
        response = new HttpResponse(409);

        response.SetBody(PkgRepo.ErrorResponse[ErrorCode.InvalidCredentials].ToString());
        serverStruct.Response = response;
        serverStruct.SendResponse();
        return true;
    }

    [HTTP("GET", "/session/verify")]
    public static bool VerifySession(HttpRequest request, ServerStruct serverStruct)
    {
        var response = new HttpResponse(200);
        if (serverStruct.Headers["Session"] != null)
        {

            if (PkgRepo.Sessions.All(i => i.Id != serverStruct.Headers["Session"]))
            {
                response = new HttpResponse(409);
                response.SetBody(PkgRepo.ErrorResponse[ErrorCode.InvalidSession].ToString());
                serverStruct.Response = response;
                serverStruct.SendResponse();
                return true;
            }
            var session = PkgRepo.Sessions.First(i => i.Id == serverStruct.Headers["Session"]);
            if (session.HasExpired())
            {
                response = new HttpResponse(409);
                response.SetBody(PkgRepo.ErrorResponse[ErrorCode.ExpiredSession].ToString());
                serverStruct.Response = response;
                serverStruct.SendResponse();
                return true;
            }
            response.SetBody(new JObject()
                {
                    {"SessionID", session.Id},

                    {"WillExpireIn", (session.ExpiryDate.Ticks - DateTime.Now.Ticks)/36000000000d},
                    {"SessionOwner", session.OwnerID}
                }.ToString());
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
}