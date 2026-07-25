namespace ATS.Application.DTOs.Users;

public record UserListItemDto(Guid Id, string FullName, string Email, string Role);
