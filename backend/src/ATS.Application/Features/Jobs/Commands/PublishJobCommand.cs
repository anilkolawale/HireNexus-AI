using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Jobs.Commands;

public record PublishJobCommand(Guid JobId, Guid RequestingUserCompanyId, bool IsSuperAdmin) : IRequest;

public class PublishJobCommandHandler : IRequestHandler<PublishJobCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly IWebhookDispatcher _webhooks;

    public PublishJobCommandHandler(IUnitOfWork uow, IWebhookDispatcher webhooks)
    {
        _uow = uow;
        _webhooks = webhooks;
    }

    public async Task Handle(PublishJobCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Job>();
        var job = await repo.Query()
            .Include(j => j.Department)
            .Include(j => j.Company)
            .FirstOrDefaultAsync(j => j.Id == request.JobId, ct)
            ?? throw new NotFoundException(nameof(Job), request.JobId);

        if (!request.IsSuperAdmin && job.CompanyId != request.RequestingUserCompanyId)
            throw new ForbiddenAccessException();

        job.Status = JobStatus.Published;
        repo.Update(job);
        await _uow.SaveChangesAsync(ct);

        await _webhooks.DispatchAsync(job.CompanyId, "job.published", new
        {
            jobId = job.Id,
            title = job.Title,
            department = job.Department.Name,
            location = job.Location,
            employmentType = job.EmploymentType.ToString(),
            publishedAtUtc = DateTime.UtcNow
        }, ct);
    }
}
