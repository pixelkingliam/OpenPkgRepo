
using System;
using System.Net;
using System.Linq;
using NetCoreServer;
using System.Reflection;
using System.Net.Sockets;
using static OpenPkgRepo.PkgRepo;
using System.Collections.Generic;
namespace OpenPkgRepo;

class HttpContext
{
    public readonly HttpRequest Request;
    public readonly Dictionary<string,string> Parameters;
    public readonly Dictionary<string,string> Headers;
    public HttpContext(HttpRequest request, Dictionary<string, string> headers)
    {
        Request = request;
        Headers = headers;
    }
    public HttpContext(HttpRequest request,Dictionary<string, string> headers, Dictionary<string, string> parameters)
    {
        Parameters = parameters;
        Request = request;
        Headers = headers;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class RouteClassAttribute : Attribute
{
}
abstract class StaticRoute
{
    public abstract string RouteUrl
    {
        get;
    }
    public virtual HttpResponse GetResponse(HttpContext ctx) { return OPRSession.ErrorServer501; }
    public virtual HttpResponse HeadResponse(HttpContext ctx) { return OPRSession.ErrorServer501; }
    public virtual HttpResponse PostResponse(HttpContext ctx) { return OPRSession.ErrorServer501; }
    public virtual HttpResponse PutResponse(HttpContext ctx) { return OPRSession.ErrorServer501; }
    public virtual HttpResponse DeleteResponse(HttpContext ctx) { return OPRSession.ErrorServer501; }
    public virtual HttpResponse ConnectResponse(HttpContext ctx) { return OPRSession.ErrorServer501; }
    public virtual HttpResponse OptionsResponse(HttpContext ctx) { return OPRSession.ErrorServer501; }
    public virtual HttpResponse TraceResponse(HttpContext ctx) { return OPRSession.ErrorServer501; }
    public virtual HttpResponse PatchResponse(HttpContext ctx) { return OPRSession.ErrorServer501; }

}
abstract class ParameterRoute : StaticRoute
{

}
class OPRServer : HttpServer
{
    public readonly List<StaticRoute> SRoutes = new();
    public readonly List<ParameterRoute> PRoutes = new();

    
    protected override TcpSession CreateSession() { return new OPRSession(this); }

    public OPRServer(IPAddress address, int port) : base(address, port)
    {
        foreach (var item in Assembly.GetExecutingAssembly().GetTypes().Where(i => (i.GetCustomAttributes(typeof(RouteClassAttribute), false).Length != 0)))
        {
            if (item.IsSubclassOf(typeof(StaticRoute)))
            {
                var sRoute = (StaticRoute)Activator.CreateInstance(item);
                LogSuccess.Print($"Loaded Static Route {sRoute?.RouteUrl}");
                SRoutes.Add(sRoute);

            }
            if (item.IsSubclassOf(typeof(ParameterRoute)))
            {
                var pRoute = (ParameterRoute)Activator.CreateInstance(item);
                LogSuccess.Print($"Loaded Parameter Route {pRoute?.RouteUrl}");
                PRoutes.Add(pRoute);

            }
        }
    }


    protected override void OnError(SocketError error)
    {
        Console.WriteLine($"HTTP session caught an error: {error}");
    }
}
class OPRSession : HttpSession
{
    public static HttpResponse ErrorServer500
    {
        get
        {
            var returnValue = new HttpResponse(500);
            returnValue.SetBody("Internal Server Error");
            return returnValue;
        }
    }
    public static HttpResponse ErrorServer501
    {
        get
        {
            var returnValue = new HttpResponse(501);
            returnValue.SetBody("Route Not Implemented");
            return returnValue;
        }
    }
    public OPRSession(HttpServer server) : base(server) { }
    protected override void OnReceivedRequest(HttpRequest request)
    {
        var sRoutes = ((OPRServer)Server).SRoutes;
        var pRoutes = ((OPRServer)Server).PRoutes;
        Dictionary<string, string> headers = new();
        for (var i = 0; i < request.Headers; i++)
        {
            headers.Add(request.Header(i).Item1,request.Header(i).Item2);
        }
        // static route handling

        bool isStatic = sRoutes.Any(i => i.RouteUrl == request.Url);
        if (isStatic)
        {
            var route = sRoutes.First(i => i.RouteUrl == request.Url);
            var routeResponse = new HttpResponse();
            switch (request.Method)
            {

                case "GET":
                {
                    try
                    {
                        routeResponse = route.GetResponse(new(request, headers));
                    }
                    catch (Exception e)
                    {
                        LogError.Print(e.Message);
                        LogError.Print(e.StackTrace);
                        SendResponse(ErrorServer500);
                    }
                    break;
                }
                case "HEAD":
                {
                    try
                    {
                        routeResponse = route.HeadResponse(new(request, headers));
                    }
                    catch (Exception e)
                    {
                        LogError.Print(e.Message);
                        LogError.Print(e.StackTrace);
                        SendResponse(ErrorServer500);
                    }
                    break;
                }
                case "POST":
                {
                    try
                    {
                        routeResponse = route.PostResponse(new(request, headers));
                    }
                    catch (Exception e)
                    {
                        LogError.Print(e.Message);
                        LogError.Print(e.StackTrace);
                        SendResponse(ErrorServer500);
                    }
                    break;
                }
                case "PUT":
                {
                    try
                    {
                        routeResponse = route.PutResponse(new(request, headers));
                    }
                    catch (Exception e)
                    {
                        LogError.Print(e.Message);
                        LogError.Print(e.StackTrace);
                        SendResponse(ErrorServer500);
                    }
                    break;
                }
                case "DELETE":
                {
                    try
                    {
                        routeResponse = route.DeleteResponse(new(request, headers));
                    }
                    catch (Exception e)
                    {
                        LogError.Print(e.Message);
                        LogError.Print(e.StackTrace);
                        SendResponse(ErrorServer500);
                    }
                    break;
                }
                case "CONNECT":
                {
                    try
                    {
                        routeResponse = route.ConnectResponse(new(request, headers));
                    }
                    catch (Exception e)
                    {
                        LogError.Print(e.Message);
                        LogError.Print(e.StackTrace);
                        SendResponse(ErrorServer500);
                    }
                    break;
                }
                case "OPTIONS":
                {
                    try
                    {
                        routeResponse = route.OptionsResponse(new(request, headers));
                    }
                    catch (Exception e)
                    {
                        LogError.Print(e.Message);
                        LogError.Print(e.StackTrace);
                        SendResponse(ErrorServer500);
                    }
                    break;
                }
                case "TRACE":
                {
                    try
                    {
                        routeResponse = route.TraceResponse(new(request, headers));
                    }
                    catch (Exception e)
                    {
                        LogError.Print(e.Message);
                        LogError.Print(e.StackTrace);
                        SendResponse(ErrorServer500);
                    }
                    break;
                }
                case "PATCH":
                {
                    try
                    {
                        routeResponse = route.PatchResponse(new(request, headers));
                    }
                    catch (Exception e)
                    {
                        LogError.Print(e.Message);
                        LogError.Print(e.StackTrace);
                        SendResponse(ErrorServer500);
                    }
                    break;
                }

            }

            SendResponse(routeResponse);
            return;
        }

        // figure out if we should call a parameter route
        
        var isParameter = false;
        var pRouteToUse = -1;
        Dictionary<string, string> parameters = new();
        for (int i = 0; i < pRoutes.Count; i++)
        {
            LogInfo.Print($"pRoute {pRoutes[i].RouteUrl.Count(f => f == '/')} | url {Request.Url.Count(f => f == '/')}");
            if (pRoutes[i].RouteUrl.Count(f => f == '/') != Request.Url.Count(f => f == '/'))
            {
                LogInfo.Print("Leaving.");
                continue;
            }
            LogInfo.Print("Continuing");
            string[] urlArr = Request.Url.Split('/');
            List<int> indices = new();


            string[] i1Arr = pRoutes[i].RouteUrl.Split('/');



            for (int i1 = 0; i1 < i1Arr.Length; i1++)
            {
                if (i1Arr[i1].StartsWith('{') && i1Arr[i1].EndsWith('}'))
                {
                    i1Arr[i1] = "";
                    indices.Add(i1);
                }
                i1Arr[i1] = "/" + i1Arr[i1];


            }
            indices.ForEach(delegate (int index)
            {
                urlArr[index] = "";
            });
            for (var i1 = 0; i1 < urlArr.Length; i1++)
            {
                urlArr[i1] = "/" + urlArr[i1];
            }


            if (string.Join('/', i1Arr) == string.Join('/', urlArr))
            {
                i1Arr = pRoutes[i].RouteUrl.Split('/');
                urlArr = Request.Url.Split('/');

                indices.ForEach(delegate (int index)
                {
                    parameters.Add(i1Arr[index].Trim('{', '}'), urlArr[index].TrimStart('/'));

                });
                isParameter = true;
                pRouteToUse = i;
                break;
            }

        }


        //bool IsParameter = PRoutes.Any(i => i.RouteURL.Split('/').Where(o => o.StartsWith('{') && o.EndsWith('}')));
        if (isParameter)
        {
            var route = pRoutes[pRouteToUse];
            var routeResponse = new HttpResponse();
            switch (request.Method)
            {

                case "GET":
                {
                    try
                    {
                        routeResponse = route.GetResponse(new(request, headers, parameters));
                    }
                    catch (Exception e)
                    {
                        LogError.Print(e.Message);
                        LogError.Print(e.StackTrace);
                        SendResponse(ErrorServer500);
                    }
                    break;
                }
                case "HEAD":
                {
                    try
                    {
                        routeResponse = route.HeadResponse(new(request, headers, parameters));
                    }
                    catch (Exception e)
                    {
                        LogError.Print(e.Message);
                        LogError.Print(e.StackTrace);
                        SendResponse(ErrorServer500);
                    }
                    break;
                }
                case "POST":
                {
                    try
                    {
                        routeResponse = route.PostResponse(new(request, headers, parameters));
                    }
                    catch (Exception e)
                    {
                        LogError.Print(e.Message);
                        LogError.Print(e.StackTrace);
                        SendResponse(ErrorServer500);
                    }
                    break;
                }
                case "PUT":
                {
                    try
                    {
                        routeResponse = route.PutResponse(new(request, headers, parameters));
                    }
                    catch (Exception e)
                    {
                        LogError.Print(e.Message);
                        LogError.Print(e.StackTrace);
                        SendResponse(ErrorServer500);
                    }
                    break;
                }
                case "DELETE":
                {
                    try
                    {
                        routeResponse = route.DeleteResponse(new(request, headers, parameters));
                    }
                    catch (Exception e)
                    {
                        LogError.Print(e.Message);
                        LogError.Print(e.StackTrace);
                        SendResponse(ErrorServer500);
                    }
                    break;
                }
                case "CONNECT":
                {
                    try
                    {
                        routeResponse = route.ConnectResponse(new(request, headers, parameters));
                    }
                    catch (Exception e)
                    {
                        LogError.Print(e.Message);
                        LogError.Print(e.StackTrace);
                        SendResponse(ErrorServer500);
                    }
                    break;
                }
                case "OPTIONS":
                {
                    try
                    {
                        routeResponse = route.OptionsResponse(new(request, headers, parameters));
                    }
                    catch (Exception e)
                    {
                        LogError.Print(e.Message);
                        LogError.Print(e.StackTrace);
                        SendResponse(ErrorServer500);
                    }
                    break;
                }
                case "TRACE":
                {
                    try
                    {
                        routeResponse = route.TraceResponse(new(request, headers, parameters));
                    }
                    catch (Exception e)
                    {
                        LogError.Print(e.Message);
                        LogError.Print(e.StackTrace);
                        SendResponse(ErrorServer500);
                    }
                    break;
                }
                case "PATCH":
                {
                    try
                    {
                        routeResponse = route.PatchResponse(new(request, headers, parameters));
                    }
                    catch (Exception e)
                    {
                        LogError.Print(e.Message);
                        LogError.Print(e.StackTrace);
                        SendResponse(ErrorServer500);
                    }
                    break;
                }
            }
            SendResponse(routeResponse);
            return;
        }

        SendResponse(ErrorServer501);
    }
}
[RouteClass]
class Main : StaticRoute
{
    public override string RouteUrl => "/main";

    public override HttpResponse GetResponse(HttpContext ctx)
    {
        var response = new HttpResponse(200);
        response.SetBody("Hello World!");
        var session = new Session(32, DateTime.Now.AddMonths(1), 1);
        LogInfo.Print("Test Session Info:");
        LogInfo.Print($"Session ID : {session.Id}");
        LogInfo.Print($"Session Owner : {session.GetAccount().Username}");
        LogInfo.Print($"Session Expired? : {session.HasExpired()}");

        return response;
    }
}

   
[RouteClass]
class TestParam : ParameterRoute
{
    public override string RouteUrl => "/add/{1}/{2}";


    public override HttpResponse GetResponse(HttpContext ctx)
    {
        var response = new HttpResponse(200);
        response.SetBody($"{Convert.ToInt32(ctx.Parameters["1"]) + Convert.ToInt32(ctx.Parameters["2"])}");
        return response;
    }
}