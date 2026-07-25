using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Companies.Commands;

public record DeleteCompanyCommand(Guid Id) : IRequest;

public class DeleteCompanyCommandHandler : IRequestHandler<DeleteCompanyCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteCompanyCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(DeleteCompanyCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Company>();
        var company = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Company), request.Id);

        var hasJobs = await _uow.Repository<Job>().ExistsAsync(j => j.CompanyId == request.Id, ct);
        if (hasJobs)
            throw new ConflictException("Cannot delete a company that has jobs posted. Close or reassign its jobs first.");

        repo.Remove(company);
        await _uow.SaveChangesAsync(ct);
    }
}
