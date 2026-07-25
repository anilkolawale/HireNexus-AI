using ATS.Application.Common.Models;
using ATS.Application.DTOs.Admin;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Admin.Queries;

public record GetAuditLogsQuery(string? EntityName, Guid? UserId, int PageNumber = 1, int PageSize = 25)
    : IRequest<PaginatedList<AuditLogRowDto>>;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PaginatedList<AuditLogRowDto>>
{
    private readonly IUnitOfWork _uow;

    public GetAuditLogsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PaginatedList<AuditLogRowDto>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        var query = _uow.Repository<AuditLog>().Query();

        if (!string.IsNullOrWhiteSpace(request.EntityName))
            query = query.Where(a => a.EntityName == request.EntityName);
        if (request.UserId.HasValue)
            query = query.Where(a => a.UserId == request.UserId);

        var users = await _uow.Repository<User>().Query().ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}", ct);

        var projected = query
            .OrderByDescending(a => a.TimestampUtc)
            .Select(a => new AuditLogRowDto(
                a.Id, a.UserId,
                a.UserId.HasValue && users.ContainsKey(a.UserId.Value) ? users[a.UserId.Value] : null,
                a.Action, a.EntityName, a.EntityId, a.TimestampUtc));

        return await PaginatedList<AuditLogRowDto>.CreateAsync(projected, request.PageNumber, request.PageSize);
    }
}
