using ATS.Application.Common.Interfaces;
using ATS.Application.DTOs.Offers;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Offers.Commands;

// Candidate accepts or declines their own offer.
public record RespondToOfferCommand(Guid OfferId, Guid CandidateUserId, bool Accept) : IRequest<OfferDto>;

public class RespondToOfferCommandHandler : IRequestHandler<RespondToOfferCommand, OfferDto>
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notifications;
    private readonly ICurrentUserService _currentUser;

    public RespondToOfferCommandHandler(IUnitOfWork uow, INotificationService notifications, ICurrentUserService currentUser)
    {
        _uow = uow;
        _notifications = notifications;
        _currentUser = currentUser;
    }

    public async Task<OfferDto> Handle(RespondToOfferCommand request, CancellationToken ct)
    {
        var offerRepo = _uow.Repository<Offer>();
        var offer = await offerRepo.Query()
            .Include(o => o.Application).ThenInclude(a => a.Job)
            .Include(o => o.Application).ThenInclude(a => a.Candidate).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(o => o.Id == request.OfferId, ct)
            ?? throw new NotFoundException(nameof(Offer), request.OfferId);

        if (_currentUser.Role == "Candidate" && offer.Application.Candidate.UserId != request.CandidateUserId)
            throw new ForbiddenAccessException();


        offer.IsAccepted = request.Accept;
        offer.RespondedAtUtc = DateTime.UtcNow;
        offerRepo.Update(offer);

        var applicationRepo = _uow.Repository<Domain.Entities.Application>();
        offer.Application.Status = request.Accept ? ApplicationStatus.Hired : ApplicationStatus.Rejected;
        applicationRepo.Update(offer.Application);

        await _uow.Repository<ApplicationStatusHistory>().AddAsync(new ApplicationStatusHistory
        {
            ApplicationId = offer.ApplicationId,
            FromStatus = ApplicationStatus.Offer,
            ToStatus = offer.Application.Status,
            ChangedByUserId = request.CandidateUserId,
            Notes = request.Accept ? "Offer accepted by candidate" : "Offer declined by candidate"
        }, ct);

        await _uow.SaveChangesAsync(ct);

        var candidateName = $"{offer.Application.Candidate.User.FirstName} {offer.Application.Candidate.User.LastName}";

        await _notifications.NotifyUserAsync(
            offer.Application.Job.CreatedByRecruiterId,
            request.Accept ? "Offer accepted!" : "Offer declined",
            $"{candidateName} has {(request.Accept ? "accepted" : "declined")} the offer for {offer.Application.Job.Title}.", ct);

        return new OfferDto(offer.Id, offer.ApplicationId, offer.Application.Job.Title, candidateName,
            offer.OfferedSalary, offer.JoiningDate, offer.Notes, offer.IsAccepted, offer.RespondedAtUtc, offer.CreatedAtUtc);
    }
}
