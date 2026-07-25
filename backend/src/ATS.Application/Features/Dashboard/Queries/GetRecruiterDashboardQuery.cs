using System.Text.Json;
using ATS.Application.DTOs.Dashboard;
using ATS.Domain.Entities;
using ApplicationEntity = ATS.Domain.Entities.Application;
using ATS.Domain.Enums;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace ATS.Application.Features.Dashboard.Queries;

public record GetRecruiterDashboardQuery(Guid? CompanyId) : IRequest<RecruiterDashboardDto>;

public class GetRecruiterDashboardQueryHandler : IRequestHandler<GetRecruiterDashboardQuery, RecruiterDashboardDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IDistributedCache _cache;

    public GetRecruiterDashboardQueryHandler(IUnitOfWork uow, IDistributedCache cache)
    {
        _uow = uow;
        _cache = cache;
    }

    public async Task<RecruiterDashboardDto> Handle(GetRecruiterDashboardQuery request, CancellationToken ct)
    {
        // Cache key is scoped by company so multi-tenant stays isolated
        var cacheKey = $"dashboard:recruiter:{request.CompanyId?.ToString() ?? "all"}";

        // Try cache first (5-minute TTL)
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
        {
            var fromCache = JsonSerializer.Deserialize<RecruiterDashboardDto>(cached);
            if (fromCache is not null) return fromCache;
        }

        IQueryable<Job> jobsQuery = _uow.Repository<Job>().Query();

        if (request.CompanyId.HasValue)
        {
            jobsQuery = jobsQuery.Where(j => j.CompanyId == request.CompanyId);
        }

        var openJobs = await jobsQuery.CountAsync(j => j.Status == JobStatus.Published, ct);

        IQueryable<ApplicationEntity> applicationsQuery = _uow.Repository<ApplicationEntity>()
            .Query()
            .Include(a => a.Job);

        if (request.CompanyId.HasValue)
        {
            applicationsQuery = applicationsQuery.Where(a => a.Job.CompanyId == request.CompanyId);
        }

        var totalApplications = await applicationsQuery.CountAsync(ct);

        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);

        var interviewsThisWeek = await _uow.Repository<Interview>()
            .Query()
            .CountAsync(i => i.ScheduledAtUtc >= weekStart &&
                             i.ScheduledAtUtc < weekStart.AddDays(7), ct);

        var offersExtended = await _uow.Repository<Offer>()
            .Query()
            .CountAsync(ct);

        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5).Date;

        var monthlyRaw = await applicationsQuery
            .Where(a => a.CreatedAtUtc >= sixMonthsAgo)
            .GroupBy(a => new { a.CreatedAtUtc.Year, a.CreatedAtUtc.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count()
            })
            .ToListAsync(ct);

        var monthlyApplications = Enumerable.Range(0, 6)
            .Select(offset => sixMonthsAgo.AddMonths(offset))
            .Select(month =>
            {
                var match = monthlyRaw.FirstOrDefault(m =>
                    m.Year == month.Year &&
                    m.Month == month.Month);

                return new MonthlyCountDto(
                    month.ToString("MMM yyyy"),
                    match?.Count ?? 0);
            })
            .ToList();

        var pipelineRaw = await applicationsQuery
            .GroupBy(a => a.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync(ct);

        var pipelineByStage = Enum.GetValues<ApplicationStatus>()
            .Select(s => new StageCountDto(
                s.ToString(),
                pipelineRaw.FirstOrDefault(p => p.Status == s)?.Count ?? 0))
            .ToList();

        var departmentHiring = await jobsQuery
            .Include(j => j.Department)
            .GroupBy(j => j.Department.Name)
            .Select(g => new DepartmentCountDto(
                g.Key,
                g.Count(j => j.Status == JobStatus.Published),
                g.SelectMany(j => j.Applications)
                    .Count(a => a.Status == ApplicationStatus.Hired)))
            .ToListAsync(ct);

        var result = new RecruiterDashboardDto(
            openJobs,
            totalApplications,
            interviewsThisWeek,
            offersExtended,
            monthlyApplications,
            pipelineByStage,
            departmentHiring);

        // Write to cache — 5 minute sliding expiry
        var json = JsonSerializer.Serialize(result);
        await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        }, ct);

        return result;
    }
}