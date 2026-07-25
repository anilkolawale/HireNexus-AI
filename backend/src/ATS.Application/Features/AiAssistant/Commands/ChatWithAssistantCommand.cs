using System.Text.Json;
using ATS.Application.Common.Interfaces;
using ATS.Application.DTOs.AiAssistant;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.AiAssistant.Commands;

// Powers the recruiter chat assistant: "Show candidates matching React and .NET",
// "Suggest a salary range", "Summarize candidate", etc. We ground the model with a compact
// snapshot of live data (open jobs + top-ranked recent applications) rather than letting it
// answer from nothing, since recruiters expect answers grounded in their actual pipeline.
public record ChatWithAssistantCommand(Guid RequestingUserId, string Message, IReadOnlyList<ChatMessageDto>? History)
    : IRequest<ChatResponseDto>;

public class ChatWithAssistantCommandValidator : AbstractValidator<ChatWithAssistantCommand>
{
    public ChatWithAssistantCommandValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
    }
}

public class ChatWithAssistantCommandHandler : IRequestHandler<ChatWithAssistantCommand, ChatResponseDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IAiService _aiService;

    public ChatWithAssistantCommandHandler(IUnitOfWork uow, IAiService aiService)
    {
        _uow = uow;
        _aiService = aiService;
    }

    public async Task<ChatResponseDto> Handle(ChatWithAssistantCommand request, CancellationToken ct)
    {
        var openJobs = await _uow.Repository<Job>().Query()
            .Where(j => j.Status == JobStatus.Published)
            .Select(j => new { j.Title, Department = j.Department.Name, j.Location, ApplicationCount = j.Applications.Count })
            .Take(20)
            .ToListAsync(ct);

        var recentTopApplications = await _uow.Repository<Domain.Entities.Application>().Query()
            .Include(a => a.Job)
            .Include(a => a.Candidate).ThenInclude(c => c.User)
            .OrderByDescending(a => a.MatchScore)
            .Take(10)
            .Select(a => new
            {
                Candidate = a.Candidate.User.FirstName + " " + a.Candidate.User.LastName,
                Job = a.Job.Title,
                a.MatchScore,
                Status = a.Status.ToString()
            })
            .ToListAsync(ct);

        var context = JsonSerializer.Serialize(new { openJobs, recentTopApplications });

        // Fold prior turns into a single prompt since IAiService.ChatAsync is single-shot;
        // a true multi-turn Messages array would replace this once the endpoint needs it.
        var historyText = request.History is null || request.History.Count == 0
            ? ""
            : string.Join("\n", request.History.Select(h => $"{h.Role}: {h.Content}")) + "\n";

        var reply = await _aiService.ChatAsync(historyText + $"user: {request.Message}", context, ct);
        return new ChatResponseDto(reply);
    }
}
