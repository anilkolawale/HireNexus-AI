using ATS.Application.DTOs.Users;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Users.Queries;

// Powers pickers like "assign interviewer" / "assign hiring manager".
public record GetUsersByRoleQuery(UserRoleType Role) : IRequest<IReadOnlyList<UserListItemDto>>;

public class GetUsersByRoleQueryHandler : IRequestHandler<GetUsersByRoleQuery, IReadOnlyList<UserListItemDto>>
{
    private readonly IUnitOfWork _uow;

    public GetUsersByRoleQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<UserListItemDto>> Handle(GetUsersByRoleQuery request, CancellationToken ct)
    {
        var users = await _uow.Repository<User>().Query()
            .Include(u => u.Role)
            .Where(u => u.Role.Type == request.Role && u.IsActive)
            .OrderBy(u => u.FirstName)
            .ToListAsync(ct);

        return users.Select(u => new UserListItemDto(u.Id, $"{u.FirstName} {u.LastName}", u.Email, u.Role.Name)).ToList();
    }
}
