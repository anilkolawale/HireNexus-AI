using ATS.Application.Common.Interfaces;
using ATS.AI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ATS.AI;

public static class DependencyInjection
{
    public static IServiceCollection AddAiServices(this IServiceCollection services)
    {
        // ILogger<AiService> is automatically provided by ASP.NET Core's logging infrastructure.
        // HttpClient is registered via AddHttpClient which also enables typed client injection.
        services.AddHttpClient<IAiService, AiService>();
        return services;
    }
}
