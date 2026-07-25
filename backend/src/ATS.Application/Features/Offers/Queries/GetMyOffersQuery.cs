using ATS.Application.Common.Interfaces;
using ATS.Application.DTOs.Offers;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Offers.Queries;

public record GetMyOffersQuery(Guid CandidateUserId) : IRequest<IReadOnlyList<OfferDto>>;

public class GetMyOffersQueryHandler : IRequestHandler<GetMyOffersQuery, IReadOnlyList<OfferDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public GetMyOffersQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<OfferDto>> Handle(GetMyOffersQuery request, CancellationToken ct)
    {
        var role = _currentUser.Role ?? string.Empty;
        var userId = _currentUser.UserId;

        var query = _uow.Repository<Offer>().Query()
            .Include(o => o.Application).ThenInclude(a => a.Job)
            .Include(o => o.Application).ThenInclude(a => a.Candidate).ThenInclude(c => c.User)
            .AsQueryable();

        if (role == "Candidate")
        {
            query = query.Where(o => o.Application.Candidate.UserId == userId);
        }

        var offers = await query.OrderByDescending(o => o.CreatedAtUtc).ToListAsync(ct);

        return offers.Select(o => new OfferDto(
            o.Id, o.ApplicationId, o.Application.Job.Title,
            $"{o.Application.Candidate.User.FirstName} {o.Application.Candidate.User.LastName}",
            o.OfferedSalary, o.JoiningDate, o.Notes, o.IsAccepted, o.RespondedAtUtc, o.CreatedAtUtc)).ToList();
    }
}

