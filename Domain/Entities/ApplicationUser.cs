using Microsoft.AspNetCore.Identity;

namespace SpektraCaseStudy.Domain.Entities;

public sealed class ApplicationUser : IdentityUser
{
    public string TenantId { get; set; } = string.Empty;
    public bool IsTenantAdmin { get; set; }
}
