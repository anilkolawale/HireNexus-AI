using System.Text.Json;
using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;

namespace ATS.Application.Common.Behaviors;

// Automatically writes an AuditLog row for every MediatR *Command* (not queries — those are
// read-only and would just add noise). Runs after the handler succeeds so failed commands
// aren't logged as if they happened. This is why individual command handlers don't need to
// write their own audit entries — one behavior covers all of them going forward.
public class AuditLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public AuditLoggingBehavior(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var response = await next();

        var requestName = typeof(TRequest).Name;
        if (!requestName.EndsWith("Command", StringComparison.Ordinal))
            return response;

        // Auth commands carry plaintext passwords in the request — never serialize those.
        if (requestName is "LoginCommand" or "RegisterCommand" or "RefreshTokenCommand")
            return response;

        try
        {
            await _uow.Repository<AuditLog>().AddAsync(new AuditLog
            {
                UserId = _currentUser.UserId,
                Action = requestName,
                EntityName = InferEntityName(requestName),
                NewValuesJson = SafeSerialize(request)
            }, ct);
            await _uow.SaveChangesAsync(ct);
        }
        catch
        {
            // Audit logging must never break the primary operation that already succeeded.
        }

        return response;
    }

    private static string InferEntityName(string commandName)
    {
        // "CreateJobCommand" -> "Job", "ScheduleInterviewCommand" -> "Interview"
        var withoutSuffix = commandName.Replace("Command", "");
        foreach (var prefix in new[] { "Create", "Update", "Delete", "Change", "Submit", "Schedule", "Apply", "Respond", "Upload" })
        {
            if (withoutSuffix.StartsWith(prefix, StringComparison.Ordinal))
                return withoutSuffix[prefix.Length..];
        }
        return withoutSuffix;
    }

    private static string? SafeSerialize(TRequest request)
    {
        try
        {
            // Truncate to keep audit rows small; full payloads aren't needed for a trail.
            var json = JsonSerializer.Serialize(request);
            return json.Length > 2000 ? json[..2000] : json;
        }
        catch
        {
            return null;
        }
    }
}
