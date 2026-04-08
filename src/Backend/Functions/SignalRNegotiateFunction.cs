using Backend.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Backend.Functions;

public sealed class SignalRNegotiateFunction
{
    [Function(nameof(Negotiate))]
    public IActionResult Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "signalr/negotiate/{nodeId}")]
        HttpRequest req,
        string nodeId,
        [SignalRConnectionInfoInput(
            HubName = SignalRConstants.HubName,
            UserId = "{nodeId}",
            ConnectionStringSetting = SignalRConstants.ConnectionSetting)]
        SignalRConnectionInfo connectionInfo)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return new BadRequestObjectResult(new { error = "nodeId is required." });
        }

        return new OkObjectResult(connectionInfo);
    }
}
