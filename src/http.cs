
using System;
using NetCoreServer;
using static OpenPkgRepo.PkgRepo;
using ModdableWebServer.Attributes;
using ModdableWebServer;
using ModdableWebServer.Helper;

namespace OpenPkgRepo;

class Main
{
    [HTTP("GET","/main")]
    public static bool MainResponse(HttpRequest request, ServerStruct serverStruct)
    {
        var response = new HttpResponse(200);
        response.SetBody("Hello World!");
        var session = new Session(32, DateTime.Now.AddMonths(1), 1);
        LogInfo.Print("Test Session Info:");
        LogInfo.Print($"Session ID : {session.Id}");
        LogInfo.Print($"Session Owner : {session.GetAccount().Username}");
        LogInfo.Print($"Session Expired? : {session.HasExpired()}");
        serverStruct.Response = response;
        serverStruct.SendResponse();
        return true;
    }
}
  
class TestParam
{

    [HTTP("GET", "/add/{param1}/{param2}")]
    public static bool GetResponse(HttpRequest request, ServerStruct serverStruct)
    {
        var response = new HttpResponse(200);
        response.SetBody($"{Convert.ToInt32(serverStruct.Parameters["param1"]) + Convert.ToInt32(serverStruct.Parameters["param2"])}");
        serverStruct.Response= response;
        serverStruct.SendResponse();
        return true;
    }
}