using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SpektraCaseStudy;
using SpektraCaseStudy.Application.DTOs;
using SpektraCaseStudy.Application.Interfaces;
using SpektraCaseStudy.Application.Services;
using SpektraCaseStudy.Domain.Entities;
using SpektraCaseStudy.Endpoints;
using SpektraCaseStudy.Infrastructure.Identity;
using SpektraCaseStudy.Infrastructure.Persistence;
using SpektraCaseStudy.Infrastructure.Repositories;
using SpektraCaseStudy.Infrastructure.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddAuthorization();

builder
    .Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddClaimsPrincipalFactory<TenantUserClaimsPrincipalFactory>();

builder.Services.Configure<SpektraCaseStudy.Infrastructure.Configuration.WorkerSettings>(
    builder.Configuration.GetSection("WorkerSettings")
);

builder.Services.AddSingleton<IHotStorage, MemoryHotStorage>();
builder.Services.AddScoped<IAggregateRepository, AggregateRepository>();
builder.Services.AddScoped<IngestionService>();
builder.Services.AddScoped<EvaluationService>();
builder.Services.AddHostedService<PersistenceWorker>();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapTenantIdentityEndpoints();
app.MapApplicationEndpoints();

app.Run();
