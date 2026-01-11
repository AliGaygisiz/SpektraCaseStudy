using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using SpektraCaseStudy.Application.DTOs;
using SpektraCaseStudy.Domain.Entities;

namespace SpektraCaseStudy.Endpoints;

public static class IdentityEndpoints
{
    public static void MapTenantIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth")
            .AddEndpointFilter(
                async (context, next) =>
                {
                    if (
                        context.HttpContext.Request.Path.Value?.EndsWith(
                            "/register",
                            StringComparison.OrdinalIgnoreCase
                        ) == true
                    )
                    {
                        return Results.NotFound();
                    }
                    return await next(context);
                }
            );

        group.MapIdentityApi<ApplicationUser>();

        group.MapPost(
            "/signup",
            async (RegisterRequest model, UserManager<ApplicationUser> userManager) =>
            {
                var newTenantId = Guid.NewGuid().ToString()[..8];

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    TenantId = newTenantId,
                    IsTenantAdmin = true,
                };

                var result = await userManager.CreateAsync(user, model.Password);

                return result.Succeeded
                    ? Results.Ok(new { message = "New Tenant Created!", tenantId = newTenantId })
                    : Results.BadRequest(result.Errors);
            }
        );

        group
            .MapPost(
                "/add-member",
                async (
                    RegisterRequest model,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    var TenantId = currentUser.FindFirstValue("TenantId");
                    var isAdmin = currentUser.FindFirstValue("IsTenantAdmin");
                    if (isAdmin == "false" || TenantId == null)
                        return Results.Unauthorized();

                    var newMember = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        TenantId = TenantId,
                        IsTenantAdmin = false,
                    };

                    var result = await userManager.CreateAsync(newMember, model.Password);
                    return result.Succeeded
                        ? Results.Ok("Member added!")
                        : Results.BadRequest(result.Errors);
                }
            )
            .RequireAuthorization();
    }
}
