using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Extensions.SignalRService;

namespace Functions;

public class SignalRNegotiateFunction
{
    [Function("SignalRNegotiate")]
    public static string Negotiate(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "api/signalr/negotiate")] HttpRequestData req,
        [SignalRConnectionInfoInput(HubName = "iot-map", ConnectionStringSetting = "AzureSignalRConnectionString")] string connectionInfo)
    {
        return connectionInfo;
    }
}
