using ATS.Application.Common.Interfaces;
using ATS.Application.Features.Auth.Commands;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Interfaces;
using ATS.UnitTests.TestHelpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace ATS.UnitTests.Application.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRepository<User>> _userRepo = new();
    private readonly Mock<IRepository<RefreshToken>> _refreshTokenRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenService> _jwt = new();

    private LoginCommandHandler CreateHandler() => new(_uow.Object, _hasher.Object, _jwt.Object);

    private User BuildUser(string email, string passwordHash, bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Jane",
        LastName = "Doe",
        Email = email,
        PasswordHash = passwordHash,
        IsActive = isActive,
        Role = new Role { Type = UserRoleType.Recruiter, Name = "Recruiter" }
    };

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsAuthResult()
    {
        var user = BuildUser("jane@ats.local", "hashed-password");
        _userRepo.Setup(r => r.Query()).Returns(new[] { user }.AsAsyncQueryable());
        _uow.Setup(u => u.Repository<User>()).Returns(_userRepo.Object);
        _uow.Setup(u => u.Repository<RefreshToken>()).Returns(_refreshTokenRepo.Object);
        _hasher.Setup(h => h.Verify("correct-password", "hashed-password")).Returns(true);
        _jwt.Setup(j => j.GenerateAccessToken(user)).Returns("access-token");
        _jwt.Setup(j => j.GenerateRefreshToken(user.Id)).Returns(new RefreshToken { Token = "refresh-token", UserId = user.Id, ExpiresAtUtc = DateTime.UtcNow.AddDays(7) });

        var result = await CreateHandler().Handle(new LoginCommand("jane@ats.local", "correct-password"), default);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.User.Email.Should().Be("jane@ats.local");
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorized()
    {
        var user = BuildUser("jane@ats.local", "hashed-password");
        _userRepo.Setup(r => r.Query()).Returns(new[] { user }.AsAsyncQueryable());
        _uow.Setup(u => u.Repository<User>()).Returns(_userRepo.Object);
        _hasher.Setup(h => h.Verify("wrong-password", "hashed-password")).Returns(false);

        var act = async () => await CreateHandler().Handle(new LoginCommand("jane@ats.local", "wrong-password"), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsUnauthorized()
    {
        var user = BuildUser("jane@ats.local", "hashed-password", isActive: false);
        _userRepo.Setup(r => r.Query()).Returns(new[] { user }.AsAsyncQueryable());
        _uow.Setup(u => u.Repository<User>()).Returns(_userRepo.Object);
        _hasher.Setup(h => h.Verify("correct-password", "hashed-password")).Returns(true);

        var act = async () => await CreateHandler().Handle(new LoginCommand("jane@ats.local", "correct-password"), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*deactivated*");
    }

    [Fact]
    public async Task Handle_UnknownEmail_ThrowsUnauthorized()
    {
        _userRepo.Setup(r => r.Query()).Returns(Array.Empty<User>().AsAsyncQueryable());
        _uow.Setup(u => u.Repository<User>()).Returns(_userRepo.Object);

        var act = async () => await CreateHandler().Handle(new LoginCommand("nobody@ats.local", "whatever"), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
