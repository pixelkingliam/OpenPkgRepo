using System;
using System.Linq;
using NetCoreServer;
using Newtonsoft.Json.Linq;
using ModdableWebServer.Attributes;
using ModdableWebServer;
using ModdableWebServer.Helper;

namespace OpenPkgRepo.Routes;

class RoutePost
{
    [HTTP("PUT", "/post")]
    public static bool PutPostResponse(HttpRequest request, ServerStruct serverStruct)
    {
        var response = new HttpResponse(200);
        Session session;
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
        }
        else
        {
            response = new HttpResponse(409);
            response.SetBody(PkgRepo.ErrorResponse[ErrorCode.InvalidSession].ToString());
            serverStruct.Response = response;
            serverStruct.SendResponse();
            return true;
        }
        session = PkgRepo.Sessions.FirstOrDefault(i => i.Id == serverStruct.Headers["Session"]);

        JObject createBody;
        try
        {
            createBody = JObject.Parse(request.Body);
        }
        catch (Exception)
        {
            response = new HttpResponse(409);
            response.SetBody(PkgRepo.ErrorResponse[ErrorCode.BadRequest].ToString());
            serverStruct.Response = response;
            serverStruct.SendResponse();
            return true;
        }
        if (!createBody.ContainsKey("Name") || !createBody.ContainsKey("Description") || !createBody.ContainsKey("Body"))
        {
            response = new HttpResponse(409);
            response.SetBody(PkgRepo.ErrorResponse[ErrorCode.BadRequest].ToString());
            serverStruct.Response = response;
            serverStruct.SendResponse();
            return true;
        }
        var post = new Post()
        {
            Owner = session.OwnerID,
            Name = (string)createBody["Name"],
            Description = (string)createBody["Description"],
            Body = (string)createBody["Body"]
        };
        serverStruct.Response = response;
        serverStruct.SendResponse();
        return true;
    }

}