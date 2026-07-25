using ATS.Application.DTOs.Dashboard;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Dashboard.Queries;

public record GetCandidateDashboardQuery(Guid CandidateUserId) : IRequest<CandidateDashboardDto>;

public class GetCandidateDashboardQueryHandler : IRequestHandler<GetCandidateDashboardQuery, CandidateDashboardDto>
{
    private readonly IUnitOfWork _uow;

    public GetCandidateDashboardQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<CandidateDashboardDto> Handle(GetCandidateDashboardQuery request, CancellationToken ct)
    {
        var applications = await _uow.Repository<Domain.Entities.Application>().Query()
            .Include(a => a.Candidate)
            .Where(a => a.Candidate.UserId == request.CandidateUserId)
            .ToListAsync(ct);

        var terminal = new[] { ApplicationStatus.Hired, ApplicationStatus.Rejected };
        var active = applications.Count(a => !terminal.Contains(a.Status));

        var interviewsScheduled = await _uow.Repository<Interview>().Query()
            .CountAsync(i => i.InterviewRound.Application.Candidate.UserId == request.CandidateUserId
                && i.ScheduledAtUtc > DateTime.UtcNow, ct);

        var offersReceived = await _uow.Repository<Offer>().Query()
            .CountAsync(o => o.Application.Candidate.UserId == request.CandidateUserId, ct);

        return new CandidateDashboardDto(applications.Count, active, interviewsScheduled, offersReceived);
    }
}
