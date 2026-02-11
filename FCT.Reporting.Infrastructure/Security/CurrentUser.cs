using FCT.Reporting.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace FCT.Reporting.Infrastructure.Security
{
    public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
    {
        public string UserId =>
            accessor.HttpContext?.User?.FindFirst("oid")?.Value
            ?? accessor.HttpContext?.User?.FindFirst("sub")?.Value
            ?? "unknown";
    }
}
