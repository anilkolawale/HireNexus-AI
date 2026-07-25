using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Jobs.Commands;

public record CloseJobCommand(Guid JobId, Guid RequestingUserCompanyId, bool IsSuperAdmin) : IRequest;

public class CloseJobCommandHandler : IRequestHandler<CloseJobCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly IWebhookDispatcher _webhooks;

    public CloseJobCommandHandler(IUnitOfWork uow, IWebhookDispatcher webhooks)
    {
        _uow = uow;
        _webhooks = webhooks;
    }

    public async Task Handle(CloseJobCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Job>();
        var job = await repo.Query().FirstOrDefaultAsync(j => j.Id == request.JobId, ct)
            ?? throw new NotFoundException(nameof(Job), request.JobId);

        if (!request.IsSuperAdmin && job.CompanyId != request.RequestingUserCompanyId)
            throw new ForbiddenAccessException();

        job.Status = JobStatus.Closed;
        repo.Update(job);
        await _uow.SaveChangesAsync(ct);

        await _webhooks.DispatchAsync(job.CompanyId, "job.closed", new { jobId = job.Id, title = job.Title, closedAtUtc = DateTime.UtcNow }, ct);
    }
}
