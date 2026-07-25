using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ATS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public OnboardingController(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow         = uow;
        _currentUser = currentUser;
    }

    // GET /api/onboarding — list hired applications with onboarding status
    [HttpGet]
    [Authorize(Roles = "HRManager,SuperAdmin,Recruiter")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId ?? Guid.Empty;

        var checklists = await _uow.Repository<OnboardingChecklist>().Query()
            .Include(c => c.Tasks)
            .Include(c => c.Application)
                .ThenInclude(a => a.Candidate).ThenInclude(c => c.User)
            .Include(c => c.Application).ThenInclude(a => a.Job)
            .Where(c => c.Application.Job.CompanyId == companyId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new
            {
                c.Id, c.Status, c.StartDate, c.CreatedAtUtc,
                TotalTasks    = c.Tasks.Count,
                CompletedTasks = c.Tasks.Count(t => t.IsCompleted),
                Candidate = new { Name = c.Application.Candidate.User.FirstName + " " + c.Application.Candidate.User.LastName, c.Application.Candidate.User.Email },
                Job       = new { c.Application.Job.Title }
            })
            .ToListAsync(ct);

        return Ok(checklists);
    }

    // GET /api/onboarding/{id} — full checklist with tasks
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetChecklist(Guid id, CancellationToken ct)
    {
        var checklist = await _uow.Repository<OnboardingChecklist>().Query()
            .Include(c => c.Tasks.OrderBy(t => t.Order))
            .Include(c => c.Application)
                .ThenInclude(a => a.Candidate).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (checklist is null) return NotFound();

        return Ok(new
        {
            checklist.Id, checklist.Status, checklist.StartDate,
            Candidate = new { Name = checklist.Application.Candidate.User.FirstName + " " + checklist.Application.Candidate.User.LastName },
            Tasks = checklist.Tasks.Select(t => new
            {
                t.Id, t.Title, t.Description, t.AssignedTo,
                t.DueDate, t.IsCompleted, t.CompletedAtUtc, t.Order
            })
        });
    }

    // POST /api/onboarding — create checklist with default tasks
    [HttpPost]
    [Authorize(Roles = "HRManager,SuperAdmin")]
    public async Task<IActionResult> CreateChecklist([FromBody] CreateChecklistRequest req, CancellationToken ct)
    {
        var checklist = new OnboardingChecklist
        {
            Id            = Guid.NewGuid(),
            ApplicationId = req.ApplicationId,
            StartDate     = req.StartDate,
            Status        = OnboardingStatus.NotStarted,
            Tasks         = new List<OnboardingTask>
            {
                new() { Id = Guid.NewGuid(), Title = "Send welcome email", AssignedTo = "HR",      Order = 1, DueDate = req.StartDate?.AddDays(-1) },
                new() { Id = Guid.NewGuid(), Title = "Set up workstation & laptop", AssignedTo = "IT",      Order = 2, DueDate = req.StartDate },
                new() { Id = Guid.NewGuid(), Title = "Create system accounts (email, Slack, JIRA)", AssignedTo = "IT", Order = 3, DueDate = req.StartDate },
                new() { Id = Guid.NewGuid(), Title = "Complete employment contract signing", AssignedTo = "NewHire", Order = 4, DueDate = req.StartDate },
                new() { Id = Guid.NewGuid(), Title = "Benefits enrollment", AssignedTo = "HR",      Order = 5, DueDate = req.StartDate?.AddDays(3) },
                new() { Id = Guid.NewGuid(), Title = "HR orientation session", AssignedTo = "HR",      Order = 6, DueDate = req.StartDate?.AddDays(1) },
                new() { Id = Guid.NewGuid(), Title = "Meet the team introduction", AssignedTo = "Manager", Order = 7, DueDate = req.StartDate?.AddDays(1) },
                new() { Id = Guid.NewGuid(), Title = "30-day check-in scheduled", AssignedTo = "Manager", Order = 8, DueDate = req.StartDate?.AddDays(30) },
            }
        };

        await _uow.Repository<OnboardingChecklist>().AddAsync(checklist, ct);
        await _uow.SaveChangesAsync(ct);
        return Ok(new { checklist.Id, TaskCount = checklist.Tasks.Count });
    }

    // POST /api/onboarding/{checklistId}/tasks — add custom task
    [HttpPost("{checklistId:guid}/tasks")]
    [Authorize(Roles = "HRManager,SuperAdmin")]
    public async Task<IActionResult> AddTask(Guid checklistId, [FromBody] AddTaskRequest req, CancellationToken ct)
    {
        var checklist = await _uow.Repository<OnboardingChecklist>().Query()
            .Include(c => c.Tasks)
            .FirstOrDefaultAsync(c => c.Id == checklistId, ct);
        if (checklist is null) return NotFound();

        var task = new OnboardingTask
        {
            Id          = Guid.NewGuid(),
            ChecklistId = checklistId,
            Title       = req.Title,
            Description = req.Description,
            AssignedTo  = req.AssignedTo,
            DueDate     = req.DueDate,
            Order       = checklist.Tasks.Count + 1
        };
        await _uow.Repository<OnboardingTask>().AddAsync(task, ct);
        await _uow.SaveChangesAsync(ct);
        return Ok(new { task.Id });
    }

    // PATCH /api/onboarding/tasks/{taskId}/complete
    [HttpPatch("tasks/{taskId:guid}/complete")]
    public async Task<IActionResult> CompleteTask(Guid taskId, CancellationToken ct)
    {
        var task = await _uow.Repository<OnboardingTask>().Query()
            .Include(t => t.Checklist).ThenInclude(c => c.Tasks)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (task is null) return NotFound();

        task.IsCompleted      = !task.IsCompleted;
        task.CompletedAtUtc   = task.IsCompleted ? DateTime.UtcNow : null;

        // Update checklist status
        var allTasks = task.Checklist.Tasks.ToList();
        var allDone  = allTasks.All(t => t.IsCompleted || t.Id == taskId && task.IsCompleted);
        task.Checklist.Status = allDone ? OnboardingStatus.Completed
            : allTasks.Any(t => t.IsCompleted) || task.IsCompleted ? OnboardingStatus.InProgress
            : OnboardingStatus.NotStarted;

        _uow.Repository<OnboardingTask>().Update(task);
        _uow.Repository<OnboardingChecklist>().Update(task.Checklist);
        await _uow.SaveChangesAsync(ct);
        return Ok(new { task.IsCompleted, task.Checklist.Status });
    }
}

public record CreateChecklistRequest(Guid ApplicationId, DateTime? StartDate);
public record AddTaskRequest(string Title, string? Description, string AssignedTo, DateTime? DueDate);
