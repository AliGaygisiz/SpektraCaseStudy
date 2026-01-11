using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SpektraCaseStudy.Domain.Entities;

namespace SpektraCaseStudy.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<UserAggregate> UserAggregates { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
            entity.Property(e => e.TenantId).HasDefaultValue(false)
        );

        builder.HasDefaultSchema("identity");

        builder
            .Entity<UserAggregate>()
            .HasKey(a => new
            {
                a.TenantId,
                a.UserId,
                a.EventName,
            });
    }
}
