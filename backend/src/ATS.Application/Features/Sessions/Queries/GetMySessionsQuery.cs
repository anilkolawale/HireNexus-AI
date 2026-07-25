using ATS.Application.DTOs.Sessions;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Sessions.Queries;

// "CurrentToken" lets the UI mark which row is *this* session, so a user doesn't accidentally
// revoke the device they're using right now without realizing it.
public record GetMySessionsQuery(Guid UserId, string? CurrentToken) : IRequest<IReadOnlyList<SessionDto>>;

public class GetMySessionsQueryHandler : IRequestHandler<GetMySessionsQuery, IReadOnlyList<SessionDto>>
{
    private readonly IUnitOfWork _uow;

    public GetMySessionsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<SessionDto>> Handle(GetMySessionsQuery request, CancellationToken ct)
    {
        var tokens = await _uow.Repository<RefreshToken>().Query()
            .Where(t => t.UserId == request.UserId && t.RevokedAtUtc == null && t.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(t => t.LastUsedAtUtc)
            .ToListAsync(ct);

        return tokens.Select(t => new SessionDto(
            t.Id, t.IpAddress, t.UserAgent, t.CreatedAtUtc, t.LastUsedAtUtc, t.ExpiresAtUtc,
            t.Token == request.CurrentToken)).ToList();
    }
}
