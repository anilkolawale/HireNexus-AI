using ATS.Application.DTOs.Admin;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Admin.Queries;

public record GetAllUsersQuery(string? SearchTerm) : IRequest<IReadOnlyList<UserManagementRowDto>>;

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, IReadOnlyList<UserManagementRowDto>>
{
    private readonly IUnitOfWork _uow;

    public GetAllUsersQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<UserManagementRowDto>> Handle(GetAllUsersQuery request, CancellationToken ct)
    {
        var query = _uow.Repository<User>().Query()
            .Include(u => u.Role)
            .Include(u => u.Company)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(u => u.Email.Contains(request.SearchTerm) || u.FirstName.Contains(request.SearchTerm) || u.LastName.Contains(request.SearchTerm));

        var users = await query.OrderByDescending(u => u.CreatedAtUtc).ToListAsync(ct);

        return users.Select(u => new UserManagementRowDto(
            u.Id, $"{u.FirstName} {u.LastName}", u.Email, u.Role.Name, u.Company?.Name,
            u.IsActive, u.IsEmailVerified, u.CreatedAtUtc)).ToList();
    }
}
