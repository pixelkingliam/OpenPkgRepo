using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetCoreServer;
using Newtonsoft.Json.Linq;
using Pixel.OakLog;

namespace OpenPkgRepo.Routes;

[RouteClass]
class RoutePost : StaticRoute
{
    public override string RouteUrl => "/post";
    public override HttpResponse PutResponse(HttpContext ctx)
    {
        var response = new HttpResponse(200);
        Session session;
        if (ctx.Headers["Session"] != null)
        {
            if (PkgRepo.Sessions.All(i => i.Id != ctx.Headers["Session"]))
            {
                response = new HttpResponse(409);
                response.SetBody(PkgRepo.ErrorResponse[ErrorCode.InvalidSession].ToString());
                return response;
            }
        }
        else
        {
            response = new HttpResponse(409);
            response.SetBody(PkgRepo.ErrorResponse[ErrorCode.InvalidSession].ToString());
            return response;
        }
        session = PkgRepo.Sessions.FirstOrDefault(i => i.Id == ctx.Headers["Session"]);

        JObject createBody;
        try
        {
            createBody = JObject.Parse(ctx.Request.Body);
        }
        catch (Exception)
        {
            response = new HttpResponse(409);
            response.SetBody(PkgRepo.ErrorResponse[ErrorCode.BadRequest].ToString());
            return response;
        }
        if (!createBody.ContainsKey("Name") || !createBody.ContainsKey("Description") || !createBody.ContainsKey("Body"))
        {
            response = new HttpResponse(409);
            response.SetBody(PkgRepo.ErrorResponse[ErrorCode.BadRequest].ToString());
            return response;
        }
        var post = new Post()
        {
            Owner = session.OwnerID,
            Name = (string)createBody["Name"],
            Description = (string)createBody["Description"],
            Body = (string)createBody["Body"]
        };
        return response;
    }

}