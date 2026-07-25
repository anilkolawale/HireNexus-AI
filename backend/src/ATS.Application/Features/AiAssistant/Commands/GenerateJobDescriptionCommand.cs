using ATS.Application.Common.Interfaces;
using MediatR;

namespace ATS.Application.Features.AiAssistant.Commands;

public record GenerateJobDescriptionCommand(string Title, string Department, string ExperienceLevel, string KeySkills)
    : IRequest<string>;

internal sealed class GenerateJobDescriptionCommandHandler : IRequestHandler<GenerateJobDescriptionCommand, string>
{
    private readonly IAiService _ai;

    public GenerateJobDescriptionCommandHandler(IAiService ai) => _ai = ai;

    public Task<string> Handle(GenerateJobDescriptionCommand request, CancellationToken ct)
        => _ai.GenerateJobDescriptionAsync(request.Title, request.Department, request.ExperienceLevel, request.KeySkills, ct);
}
