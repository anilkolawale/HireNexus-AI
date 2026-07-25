using ATS.Application.DTOs.Candidates;
using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Candidates.Queries;

public record GetResumeHistoryQuery(Guid CandidateUserId) : IRequest<IReadOnlyList<ResumeHistoryRowDto>>;

public class GetResumeHistoryQueryHandler : IRequestHandler<GetResumeHistoryQuery, IReadOnlyList<ResumeHistoryRowDto>>
{
    private readonly IUnitOfWork _uow;

    public GetResumeHistoryQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<ResumeHistoryRowDto>> Handle(GetResumeHistoryQuery request, CancellationToken ct)
    {
        var candidate = await _uow.Repository<Candidate>().Query()
            .FirstOrDefaultAsync(c => c.UserId == request.CandidateUserId, ct)
            ?? throw new NotFoundException(nameof(Candidate), request.CandidateUserId);

        var files = await _uow.Repository<FileAsset>().Query()
            .Where(f => f.CandidateId == candidate.Id)
            .OrderByDescending(f => f.Version)
            .ToListAsync(ct);

        return files.Select(f => new ResumeHistoryRowDto(
            f.Id, f.FileName, f.Version, f.BlobUrl, f.UploadedAtUtc, f.Id == candidate.ResumeFileId)).ToList();
    }
}
