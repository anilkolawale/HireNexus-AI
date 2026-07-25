using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;

namespace ATS.Application.Features.Companies.Commands;

public record UploadCompanyLogoCommand(Guid CompanyId, Stream FileStream, string FileName, string ContentType)
    : IRequest<string>;

public class UploadCompanyLogoCommandHandler : IRequestHandler<UploadCompanyLogoCommand, string>
{
    private readonly IUnitOfWork _uow;
    private readonly IBlobStorageService _blobStorage;

    public UploadCompanyLogoCommandHandler(IUnitOfWork uow, IBlobStorageService blobStorage)
    {
        _uow = uow;
        _blobStorage = blobStorage;
    }

    public async Task<string> Handle(UploadCompanyLogoCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Company>();
        var company = await repo.GetByIdAsync(request.CompanyId, ct)
            ?? throw new NotFoundException(nameof(Company), request.CompanyId);

        var previousLogoUrl = company.LogoUrl;

        var logoUrl = await _blobStorage.UploadAsync(request.FileStream, request.FileName, request.ContentType, ct);
        company.LogoUrl = logoUrl;
        repo.Update(company);
        await _uow.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(previousLogoUrl))
        {
            // Best-effort cleanup of the old logo; a failure here shouldn't roll back the
            // already-saved new logo URL.
            try { await _blobStorage.DeleteAsync(previousLogoUrl, ct); } catch { /* ignore */ }
        }

        return logoUrl;
    }
}
