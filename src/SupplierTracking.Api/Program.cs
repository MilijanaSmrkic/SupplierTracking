using System.Reflection;
using System.Threading.RateLimiting;
using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Serilog;
using SupplierTracking.Api.Middleware;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Behaviours;
using SupplierTracking.Domain.Entities;
using SupplierTracking.Infrastructure;
using SupplierTracking.Infrastructure.Hubs;
using SupplierTracking.Infrastructure.Jobs;
using SupplierTracking.Infrastructure.Persistence;

// Local startup logger — not stored in Log.Logger (static) so
// WebApplicationFactory can create multiple host instances without hitting
// the "already frozen" ReloadableLogger error.
using var startupLogger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog reads full config from appsettings.json after DI is ready
    builder.Host.UseSerilog((ctx, config) =>
        config.ReadFrom.Configuration(ctx.Configuration));

    // CORS — allow configured origins (set AllowedOrigins in appsettings per environment)
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
        {
            var origins = builder.Configuration
                .GetSection("AllowedOrigins")
                .Get<string[]>() ?? [];

            if (origins.Length > 0)
                policy.WithOrigins(origins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials(); // required for SignalR
            else
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
        });
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title   = "Supplier Tracking API",
            Version = "v1",
            Description = "Real-time supplier order tracking with SignalR, Hangfire and WebHooks."
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name         = "Authorization",
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            In           = ParameterLocation.Header,
            Description  = "Enter JWT token: Bearer {token}"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                        { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                []
            }
        });

        // Include XML doc comments from the API assembly
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    });

    var applicationAssembly = typeof(SupplierTracking.Application.Auth.LoginCommand).Assembly;

    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(applicationAssembly);
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
    });

    builder.Services.AddValidatorsFromAssembly(applicationAssembly);
    builder.Services.AddInfrastructure(builder.Configuration);

    // Health checks — GET /health
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<SupplierTracking.Infrastructure.Persistence.ApplicationDbContext>("database");

    // Rate limiting — 10 login attempts per IP per minute
    builder.Services.AddRateLimiter(options =>
    {
        options.AddPolicy("login", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit         = 10,
                    Window              = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit          = 0
                }));

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (ctx, _) =>
        {
            ctx.HttpContext.Response.ContentType = "application/json";
            await ctx.HttpContext.Response.WriteAsync(
                """{"status":429,"message":"Too many login attempts. Please wait a minute and try again."}""");
        };
    });

    var app = builder.Build();

    await SeedAdminAsync(app);

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Supplier Tracking API v1"));
        app.UseHangfireDashboard("/hangfire");
    }

    app.UseRateLimiter();
    app.UseCors("Frontend");
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0} ms";
    });
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<OrderHub>("/hubs/orders");
    app.MapHealthChecks("/health");

    HangfireJobRegistrar.RegisterRecurringJobs();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    startupLogger.Fatal(ex, "Application failed to start.");
    throw;
}

static async Task SeedAdminAsync(WebApplication app)
{
    using var scope  = app.Services.CreateScope();
    var context      = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
    var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
    var logger       = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("SeedAdmin");

    var adminExists = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
        .AnyAsync(context.Users, u => u.UserName == "admin");

    if (!adminExists)
    {
        context.Users.Add(new User
        {
            UserName     = "admin",
            Email        = "admin@suppliertracking.com",
            PasswordHash = tokenService.HashPassword("Admin123!"),
            Role         = UserRoles.Admin
        });
        await context.SaveChangesAsync();
        logger.LogInformation("Admin user seeded (admin@suppliertracking.com)");
    }
    else
    {
        logger.LogDebug("Admin user already exists — skipping seed");
    }
}

public partial class Program { }
