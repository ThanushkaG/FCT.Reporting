using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FCT.Reporting.Api.Hubs
{
    [Authorize]
    public class ReportsHub : Hub
    {
    }
}
