using ATS.Application.Common.Interfaces;
using ATS.Application.DTOs.Offers;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Offers.Commands;

public record CreateOfferCommand(Guid ApplicationId, decimal OfferedSalary, DateTime JoiningDate, string? Notes)
    : IRequest<OfferDto>;

public class CreateOfferCommandValidator : AbstractValidator<CreateOfferCommand>
{
    public CreateOfferCommandValidator()
    {
        RuleFor(x => x.OfferedSalary).GreaterThan(0);
        RuleFor(x => x.JoiningDate).GreaterThan(DateTime.UtcNow.Date);
    }
}

public class CreateOfferCommandHandler : IRequestHandler<CreateOfferCommand, OfferDto>
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notifications;
    private readonly IEmailService _email;
    private readonly IAiService _aiService;
    private readonly IWebhookDispatcher _webhooks;

    public CreateOfferCommandHandler(IUnitOfWork uow, INotificationService notifications, IEmailService email, IAiService aiService, IWebhookDispatcher webhooks)
    {
        _uow = uow;
        _notifications = notifications;
        _email = email;
        _aiService = aiService;
        _webhooks = webhooks;
    }

    public async Task<OfferDto> Handle(CreateOfferCommand request, CancellationToken ct)
    {
        var application = await _uow.Repository<Domain.Entities.Application>().Query()
            .Include(a => a.Job)
            .Include(a => a.Candidate).ThenInclude(c => c.User)
            .Include(a => a.Offer)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Application), request.ApplicationId);

        if (application.Offer is not null)
            throw new ConflictException("An offer has already been extended for this application.");

        var offer = new Offer
        {
            ApplicationId = application.Id,
            OfferedSalary = request.OfferedSalary,
            JoiningDate = request.JoiningDate,
            Notes = request.Notes
        };
        await _uow.Repository<Offer>().AddAsync(offer, ct);

        application.Status = ApplicationStatus.Offer;
        _uow.Repository<Domain.Entities.Application>().Update(application);

        await _uow.Repository<ApplicationStatusHistory>().AddAsync(new ApplicationStatusHistory
        {
            ApplicationId = application.Id,
            FromStatus = application.Status,
            ToStatus = ApplicationStatus.Offer,
            ChangedByUserId = application.Job.CreatedByRecruiterId,
            Notes = "Offer extended"
        }, ct);

        await _uow.SaveChangesAsync(ct);

        var candidateName = $"{application.Candidate.User.FirstName} {application.Candidate.User.LastName}";

        // AI-drafted offer email — recruiter can still edit the template server-side later if needed.
        var emailBody = await _aiService.GenerateEmailAsync(
            "job offer",
            $"Candidate: {candidateName}, Role: {application.Job.Title}, Salary: {request.OfferedSalary:C}, Joining: {request.JoiningDate:d}", ct);

        await _notifications.NotifyUserAsync(
            application.Candidate.UserId, "You have a new offer!",
            $"You've received an offer for {application.Job.Title}. Check your email for details.", ct);

        await _email.SendAsync(application.Candidate.User.Email, $"Offer: {application.Job.Title}", emailBody, ct);

        await _webhooks.DispatchAsync(application.Job.CompanyId, "offer.extended", new
        {
            offerId = offer.Id,
            applicationId = application.Id,
            jobTitle = application.Job.Title,
            candidateEmail = application.Candidate.User.Email,
            offeredSalary = offer.OfferedSalary,
            joiningDate = offer.JoiningDate
        }, ct);

        return new OfferDto(offer.Id, application.Id, application.Job.Title, candidateName,
            offer.OfferedSalary, offer.JoiningDate, offer.Notes, offer.IsAccepted, offer.RespondedAtUtc, offer.CreatedAtUtc);
    }
}
