using System.Security.Claims;
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
public class RequisitionsController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notify;

    public RequisitionsController(IUnitOfWork uow, ICurrentUserService currentUser, INotificationService notify)
    {
        _uow         = uow;
        _currentUser = currentUser;
        _notify      = notify;
    }

    // GET /api/requisitions
    [HttpGet]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId ?? Guid.Empty;
        var reqs = await _uow.Repository<JobRequisition>().Query()
            .Include(r => r.RequestedBy)
            .Include(r => r.ApprovalSteps).ThenInclude(s => s.Approver)
            .Where(r => r.CompanyId == companyId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new
            {
                r.Id, r.Title, r.Department, r.Status, r.HeadcountRequested,
                r.BudgetMin, r.BudgetMax, r.CreatedAtUtc, r.RejectionReason,
                RequestedBy = new { Name = r.RequestedBy.FirstName + " " + r.RequestedBy.LastName, r.RequestedBy.Email },
                ApprovalSteps = r.ApprovalSteps.OrderBy(s => s.StepOrder).Select(s => new
                {
                    s.Id, s.StepName, s.StepOrder, s.Status, s.Comment, s.ActedAtUtc,
                    Approver = new { Name = s.Approver.FirstName + " " + s.Approver.LastName, s.Approver.Email }
                })
            })
            .ToListAsync(ct);
        return Ok(reqs);
    }

    // POST /api/requisitions
    [HttpPost]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateRequisitionRequest req, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId ?? Guid.Empty;
        var userId    = _currentUser.UserId ?? Guid.Empty;

        var requisition = new JobRequisition
        {
            Id                 = Guid.NewGuid(),
            CompanyId          = companyId,
            RequestedById      = userId,
            Title              = req.Title,
            Department         = req.Department,
            Description        = req.Description,
            BudgetMin          = req.BudgetMin,
            BudgetMax          = req.BudgetMax,
            HeadcountRequested = req.HeadcountRequested,
            Status             = RequisitionStatus.Draft
        };

        await _uow.Repository<JobRequisition>().AddAsync(requisition, ct);
        await _uow.SaveChangesAsync(ct);
        return Ok(new { requisition.Id });
    }

    // POST /api/requisitions/{id}/submit
    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> Submit(Guid id, [FromBody] SubmitRequisitionRequest req, CancellationToken ct)
    {
        var requisition = await _uow.Repository<JobRequisition>().Query()
            .FirstOrDefaultAsync(r => r.Id == id, ct);
        if (requisition is null) return NotFound();

        // Build approval chain from request
        int order = 1;
        foreach (var step in req.ApprovalChain)
        {
            requisition.ApprovalSteps.Add(new RequisitionApprovalStep
            {
                Id          = Guid.NewGuid(),
                ApproverId  = step.ApproverId,
                StepName    = step.StepName,
                StepOrder   = order++,
                Status      = ApprovalStepStatus.Pending
            });
        }
        requisition.Status = RequisitionStatus.PendingManagerApproval;

        _uow.Repository<JobRequisition>().Update(requisition);
        await _uow.SaveChangesAsync(ct);

        // Notify the first approver
        var firstApprover = req.ApprovalChain.FirstOrDefault();
        if (firstApprover is not null)
        {
            await _notify.NotifyUserAsync(firstApprover.ApproverId,
                "Job Requisition Awaiting Your Approval",
                $"A new requisition '{requisition.Title}' requires your approval.",
                ct);
        }

        return Ok(new { requisition.Status });
    }

    // POST /api/requisitions/{id}/approve
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveRequest req, CancellationToken ct)
    {
        return await ActOnRequisition(id, true, req.Comment, ct);
    }

    // POST /api/requisitions/{id}/reject
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ApproveRequest req, CancellationToken ct)
    {
        return await ActOnRequisition(id, false, req.Comment, ct);
    }

    private async Task<IActionResult> ActOnRequisition(Guid id, bool approved, string? comment, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var req = await _uow.Repository<JobRequisition>().Query()
            .Include(r => r.ApprovalSteps).ThenInclude(s => s.Approver)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
        if (req is null) return NotFound();

        var myStep = req.ApprovalSteps
            .Where(s => s.ApproverId == userId && s.Status == ApprovalStepStatus.Pending)
            .OrderBy(s => s.StepOrder)
            .FirstOrDefault();
        if (myStep is null) return BadRequest("No pending approval step found for you.");

        myStep.Status      = approved ? ApprovalStepStatus.Approved : ApprovalStepStatus.Rejected;
        myStep.Comment     = comment;
        myStep.ActedAtUtc  = DateTime.UtcNow;

        if (!approved)
        {
            req.Status          = RequisitionStatus.Rejected;
            req.RejectionReason = comment;
        }
        else
        {
            // Advance to next pending step or mark fully Approved
            var nextStep = req.ApprovalSteps
                .Where(s => s.Status == ApprovalStepStatus.Pending && s.StepOrder > myStep.StepOrder)
                .OrderBy(s => s.StepOrder)
                .FirstOrDefault();

            if (nextStep is not null)
            {
                // Notify next approver
                await _notify.NotifyUserAsync(nextStep.ApproverId,
                    "Job Requisition Awaiting Your Approval",
                    $"Requisition '{req.Title}' is awaiting your approval.",
                    ct);
                req.Status = nextStep.StepName switch
                {
                    "Finance" => RequisitionStatus.PendingFinanceApproval,
                    "HR"      => RequisitionStatus.PendingHRApproval,
                    _         => RequisitionStatus.PendingManagerApproval
                };
            }
            else
            {
                req.Status = RequisitionStatus.Approved;
                await _notify.NotifyUserAsync(req.RequestedById,
                    "Job Requisition Approved! 🎉",
                    $"Your requisition '{req.Title}' has been fully approved. You can now post the job.",
                    ct);
            }
        }

        _uow.Repository<JobRequisition>().Update(req);
        await _uow.SaveChangesAsync(ct);
        return Ok(new { req.Status });
    }
}

public record CreateRequisitionRequest(string Title, string? Department, string? Description,
    decimal? BudgetMin, decimal? BudgetMax, int HeadcountRequested);
public record SubmitRequisitionRequest(List<ApprovalChainStep> ApprovalChain);
public record ApprovalChainStep(Guid ApproverId, string StepName);
public record ApproveRequest(string? Comment);
