namespace ATS.Application.DTOs.Auth;

public record AuthResultDto(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAtUtc, UserDto User);

public record UserDto(Guid Id, string FirstName, string LastName, string Email, string Role, bool IsEmailVerified, Guid? CompanyId = null);

