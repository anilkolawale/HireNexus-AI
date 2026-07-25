using ATS.Application.Common.Interfaces;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Applications.Queries;

public record GetSkillGapQuery(Guid ApplicationId) : IRequest<SkillGapResult>;

internal sealed class GetSkillGapQueryHandler : IRequestHandler<GetSkillGapQuery, SkillGapResult>
{
    private readonly IUnitOfWork _uow;
    private readonly IAiService _ai;

    public GetSkillGapQueryHandler(IUnitOfWork uow, IAiService ai)
    {
        _uow = uow;
        _ai = ai;
    }

    public async Task<SkillGapResult> Handle(GetSkillGapQuery request, CancellationToken ct)
    {
        var application = await _uow.Repository<Domain.Entities.Application>()
            .Query()
            .Include(a => a.Candidate)
                .ThenInclude(c => c.Skills)
            .Include(a => a.Job)
                .ThenInclude(j => j.JobSkills)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct)
            ?? throw new KeyNotFoundException($"Application {request.ApplicationId} not found.");

        var resumeText = application.Candidate.AiProfileSummary
            ?? string.Join(", ", application.Candidate.Skills.Select(s => s.SkillName));

        return await _ai.AnalyzeSkillGapAsync(resumeText, application.Job.Description, application.Job.Title, ct);
    }
}
