using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Applications.Commands;

public record ChangeApplicationStatusCommand(
    Guid ApplicationId,
    ApplicationStatus NewStatus,
    Guid ChangedByUserId,
    string? Notes) : IRequest;

public class ChangeApplicationStatusCommandHandler : IRequestHandler<ChangeApplicationStatusCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notifications;
    private readonly IWebhookDispatcher _webhooks;

    public ChangeApplicationStatusCommandHandler(IUnitOfWork uow, INotificationService notifications, IWebhookDispatcher webhooks)
    {
        _uow = uow;
        _notifications = notifications;
        _webhooks = webhooks;
    }

    public async Task Handle(ChangeApplicationStatusCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Domain.Entities.Application>();
        var application = await repo.Query()
            .Include(a => a.Candidate).ThenInclude(c => c.User)
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Application), request.ApplicationId);

        var oldStatus = application.Status;
        application.Status = request.NewStatus;
        repo.Update(application);

        await _uow.Repository<ApplicationStatusHistory>().AddAsync(new ApplicationStatusHistory
        {
            ApplicationId = application.Id,
            FromStatus = oldStatus,
            ToStatus = request.NewStatus,
            ChangedByUserId = request.ChangedByUserId,
            Notes = request.Notes
        }, ct);

        await _uow.SaveChangesAsync(ct);

        await _notifications.NotifyUserAsync(
            application.Candidate.UserId,
            "Application status updated",
            $"Your application for {application.Job.Title} is now: {request.NewStatus}.",
            ct);

        await _webhooks.DispatchAsync(application.Job.CompanyId, "application.status_changed", new
        {
            applicationId = application.Id,
            jobTitle = application.Job.Title,
            candidateEmail = application.Candidate.User.Email,
            fromStatus = oldStatus.ToString(),
            toStatus = request.NewStatus.ToString()
        }, ct);

        if (request.NewStatus == ApplicationStatus.Hired)
        {
            await _webhooks.DispatchAsync(application.Job.CompanyId, "candidate.hired", new
            {
                applicationId = application.Id,
                jobTitle = application.Job.Title,
                candidateEmail = application.Candidate.User.Email,
                hiredAtUtc = DateTime.UtcNow
            }, ct);
        }
    }
}
