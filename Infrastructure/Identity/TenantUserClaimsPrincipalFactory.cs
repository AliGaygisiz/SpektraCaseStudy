using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

using SpektraCaseStudy.Domain.Entities;

namespace SpektraCaseStudy.Infrastructure.Identity;

public class TenantUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
{
    public TenantUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> optionsAccessor
    )
        : base(userManager, optionsAccessor) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        identity.AddClaim(new Claim("TenantId", user.TenantId ?? string.Empty));
        string isAdmin = "false";
        if (user.IsTenantAdmin)
        {
            isAdmin = "true";
        }
        identity.AddClaim(new Claim("IsTenantAdmin", isAdmin));

        return identity;
    }
}
