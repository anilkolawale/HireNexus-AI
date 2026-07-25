using System.Security.Claims;
using ATS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ATS.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid? UserId
    {
        get
        {
            var id = _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? _accessor.HttpContext?.User?.FindFirstValue("sub");
            return Guid.TryParse(id, out var guid) ? guid : null;
        }
    }

    public string? Email => _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);
    public string? Role => _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);

    public Guid? CompanyId
    {
        get
        {
            var id = _accessor.HttpContext?.User?.FindFirstValue("companyId");
            return Guid.TryParse(id, out var guid) ? guid : null;
        }
    }
}
