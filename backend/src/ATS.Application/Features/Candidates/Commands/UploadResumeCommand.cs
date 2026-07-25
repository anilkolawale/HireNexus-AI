using ATS.Application.Common.Interfaces;
using ATS.Application.DTOs.Candidates;
using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Candidates.Commands;

// Stream must be seekable/re-readable by the caller (controller buffers to MemoryStream).
public record UploadResumeCommand(Guid UserId, Stream FileStream, string FileName, string ContentType)
    : IRequest<ResumeUploadResultDto>;

public class UploadResumeCommandHandler : IRequestHandler<UploadResumeCommand, ResumeUploadResultDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IBlobStorageService _blobStorage;
    private readonly IAiService _aiService;

    public UploadResumeCommandHandler(IUnitOfWork uow, IBlobStorageService blobStorage, IAiService aiService)
    {
        _uow = uow;
        _blobStorage = blobStorage;
        _aiService = aiService;
    }

    public async Task<ResumeUploadResultDto> Handle(UploadResumeCommand request, CancellationToken ct)
    {
        var candidateRepo = _uow.Repository<Candidate>();
        var candidate = await candidateRepo.Query()
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, ct);

        var isNewCandidate = candidate is null;
        if (candidate is null)
        {
            var user = await _uow.Repository<User>().GetByIdAsync(request.UserId, ct)
                ?? throw new NotFoundException(nameof(User), request.UserId);
            candidate = new Candidate { UserId = request.UserId, User = user };
            await candidateRepo.AddAsync(candidate, ct);
        }

        // 1. Upload raw file to Blob Storage (source of truth / re-download link)
        request.FileStream.Position = 0;
        var blobUrl = await _blobStorage.UploadAsync(request.FileStream, request.FileName, request.ContentType, ct);

        var fileAsset = new FileAsset
        {
            FileName = request.FileName,
            BlobUrl = blobUrl,
            ContentType = request.ContentType,
            SizeBytes = request.FileStream.Length,
            Version = (candidate.ResumeFile?.Version ?? 0) + 1,
            UploadedByUserId = request.UserId,
            CandidateId = candidate.Id
        };
        await _uow.Repository<FileAsset>().AddAsync(fileAsset, ct);

        // 2. AI parse: extract skills / education / experience / certifications / missing fields
        request.FileStream.Position = 0;
        var parsed = await _aiService.ParseResumeAsync(request.FileStream, request.FileName, ct);

        candidate.ResumeFile = fileAsset;
        candidate.Summary = string.IsNullOrWhiteSpace(candidate.Summary) ? parsed.Summary : candidate.Summary;

        // Replace AI-extracted skills (keep any manually-added, non-AI skills the candidate already had)
        var existingSkills = _uow.Repository<CandidateSkill>().Query()
            .Where(s => s.CandidateId == candidate.Id && s.ExtractedByAi).ToList();
        foreach (var s in existingSkills) _uow.Repository<CandidateSkill>().Remove(s);

        foreach (var skillName in parsed.Skills.Distinct())
        {
            await _uow.Repository<CandidateSkill>().AddAsync(new CandidateSkill
            {
                CandidateId = candidate.Id,
                SkillName = skillName,
                ExtractedByAi = true
            }, ct);
        }

        if (!isNewCandidate)
        {
            candidateRepo.Update(candidate);
        }
        await _uow.SaveChangesAsync(ct);


        return new ResumeUploadResultDto(blobUrl, parsed.Skills, parsed.MissingFields, parsed.Summary);
    }
}
