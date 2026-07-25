namespace ATS.Application.DTOs.Offers;

public record OfferDto(
    Guid Id,
    Guid ApplicationId,
    string JobTitle,
    string CandidateName,
    decimal OfferedSalary,
    DateTime JoiningDate,
    string? Notes,
    bool IsAccepted,
    DateTime? RespondedAtUtc,
    DateTime CreatedAtUtc);

public record CreateOfferDto(Guid ApplicationId, decimal OfferedSalary, DateTime JoiningDate, string? Notes);

public record RespondToOfferDto(bool Accept);
