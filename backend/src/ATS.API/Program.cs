using System.Text;
using System.Threading.RateLimiting;
using ATS.AI;
using ATS.API.Middleware;
using ATS.Infrastructure;
using ATS.Infrastructure.Jobs;
using ATS.Infrastructure.Notifications;
using ATS.Infrastructure.Persistence.Seed;
using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

#region Logging

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

#endregion

#region Services

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ATS.Application.Features.Auth.Commands.LoginCommand).Assembly);

    cfg.AddOpenBehavior(typeof(ATS.Application.Common.Behaviors.AuditLoggingBehavior<,>));
});

// Fluent Validation
builder.Services.AddValidatorsFromAssembly(
    typeof(ATS.Application.Features.Auth.Commands.LoginCommand).Assembly);

// AutoMapper
builder.Services.AddAutoMapper(
    typeof(ATS.Application.Features.Auth.Commands.LoginCommand).Assembly);

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// AI Services
builder.Services.AddAiServices();

#endregion

#region Authentication

var jwtSecret = builder.Configuration["Jwt:Secret"];

if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new Exception("JWT Secret is missing from appsettings.json");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

#endregion

#region Controllers

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

#endregion

#region Swagger

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AI Recruitment ATS API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter JWT Token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

#endregion

#region CORS

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(
                builder.Configuration["Cors:AllowedOrigin"]
                ?? "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

#endregion

#region Rate Limiting

// Global: 100 requests/minute per client IP — generic API abuse protection.
// "auth" policy: 5 requests/minute per IP — applied to login/register/forgot-password/
// reset-password, since credential-stuffing and enumeration attacks target these specifically
// and the per-account lockout in LoginCommandHandler alone doesn't stop a distributed attack
// spreading across many different email addresses from the same source.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

#endregion

var app = builder.Build();

#region Middleware

// Global Exception Middleware
app.UseMiddleware<ExceptionMiddleware>();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// HTTPS
app.UseHttpsRedirection();

// CORS
app.UseCors("FrontendPolicy");

// Static files (for local uploads fallback)
app.UseStaticFiles();

// Authentication
app.UseAuthentication();

// Authorization
app.UseAuthorization();

// Hangfire Dashboard — unrestricted in dev, SuperAdmin-only in production
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter(app.Environment) },
    DashboardTitle = "HireNexus AI — Background Jobs"
});

// Register recurring housekeeping jobs
RecurringJob.AddOrUpdate<HousekeepingJobs>(
    "purge-expired-refresh-tokens",
    job => job.PurgeExpiredRefreshTokensAsync(),
    "0 2 * * *");  // Nightly at 02:00 UTC

RecurringJob.AddOrUpdate<HousekeepingJobs>(
    "purge-expired-email-tokens",
    job => job.PurgeExpiredEmailTokensAsync(),
    "0 * * * *");  // Every hour

RecurringJob.AddOrUpdate<HousekeepingJobs>(
    "close-expired-jobs",
    job => job.CloseExpiredJobsAsync(),
    "0 0 * * *");  // Midnight UTC

RecurringJob.AddOrUpdate<HousekeepingJobs>(
    "send-interview-reminders",
    job => job.SendInterviewRemindersAsync(),
    "0 * * * *");  // Every hour — sends 24h-ahead reminders

// Rate Limiting
app.UseRateLimiter();

#endregion

#region Endpoints

app.MapControllers();

app.MapHub<NotificationHub>("/hubs/notifications");

#endregion

#region Database Seed

await DbSeeder.SeedAsync(app.Services);
await DemoDataSeeder.SeedAsync(app.Services);

#endregion

app.Run();

public partial class Program
{
}