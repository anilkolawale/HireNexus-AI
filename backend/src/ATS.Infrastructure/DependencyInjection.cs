using ATS.Application.Common.Interfaces;
using ATS.Domain.Interfaces;
using ATS.Infrastructure.Email;
using ATS.Infrastructure.Identity;
using ATS.Infrastructure.Notifications;
using ATS.Infrastructure.Persistence;
using ATS.Infrastructure.Reports;
using ATS.Infrastructure.Services;
using ATS.Infrastructure.Storage;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ATS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AtsDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AtsDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
        services.AddScoped<INotificationService, SignalRNotificationService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddHttpClient<IWebhookDispatcher, ATS.Infrastructure.Webhooks.WebhookDispatcher>();
        services.AddScoped<IReportExportService, ReportExportService>();
        services.AddSignalR();
        services.AddHttpContextAccessor();

        // Phase 6: Industry Dominance Services
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IJobBoardPublisher, JobBoardPublisher>();
        services.AddScoped<IAutomationEngine, AutomationEngine>();
        services.AddScoped<IAssessmentService, HackerRankService>();

        // Named HTTP clients for external job board APIs
        services.AddHttpClient("LinkedInJobs", c => { c.Timeout = TimeSpan.FromSeconds(30); });
        services.AddHttpClient("HackerRank", c => { c.Timeout = TimeSpan.FromSeconds(30); });


        // Redis distributed cache
        var redisConnectionString = config["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "ATS:";
            });
        }
        else
        {
            // Fallback to in-memory cache for local dev without Redis
            services.AddDistributedMemoryCache();
        }

        // Hangfire — background jobs stored in SQL Server
        services.AddHangfire(hf =>
        {
            hf.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
              .UseSimpleAssemblyNameTypeSerializer()
              .UseRecommendedSerializerSettings()
              .UseSqlServerStorage(config.GetConnectionString("DefaultConnection"));
        });
        services.AddHangfireServer();

        return services;
    }
}
