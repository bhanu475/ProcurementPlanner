using Microsoft.AspNetCore.Authorization;
using ProcurementPlanner.Core.Entities;

namespace ProcurementPlanner.API.Authorization;

public class AuthorizeRoleAttribute : AuthorizeAttribute
{
    public AuthorizeRoleAttribute(params UserRole[] roles)
    {
        Roles = string.Join(",", roles.Select(r => r.ToString()));
    }
}