using Microsoft.AspNetCore.SignalR;

namespace RagEvaluator.API.Hubs
{
    /// <summary>
    /// Hub for pushing background-job updates to clients. Server-to-client broadcast only.
    /// </summary>
    public class JobsHub : Hub
    {
    }
}
