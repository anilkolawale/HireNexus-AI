using ATS.Application.DTOs.Admin;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Admin.Queries;

public record GetAdminDashboardQuery : IRequest<AdminDashboardDto>;

public class GetAdminDashboardQueryHandler : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    private readonly IUnitOfWork _uow;

    public GetAdminDashboardQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<AdminDashboardDto> Handle(GetAdminDashboardQuery request, CancellationToken ct)
    {
        var totalUsers = await _uow.Repository<User>().Query().CountAsync(ct);
        var totalCompanies = await _uow.Repository<Company>().Query().CountAsync(ct);
        var totalJobs = await _uow.Repository<Job>().Query().CountAsync(ct);
        var totalApplications = await _uow.Repository<Domain.Entities.Application>().Query().CountAsync(ct);

        var usersByRole = await _uow.Repository<User>().Query()
            .Include(u => u.Role)
            .GroupBy(u => u.Role.Name)
            .Select(g => new RoleCountDto(g.Key, g.Count()))
            .ToListAsync(ct);

        return new AdminDashboardDto(totalUsers, totalCompanies, totalJobs, totalApplications, usersByRole);
    }
}
