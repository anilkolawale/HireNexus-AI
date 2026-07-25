using ATS.Application.Common.Interfaces;
using ATS.Application.Features.Applications.Commands;
using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using ATS.UnitTests.TestHelpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace ATS.UnitTests.Application.Applications;

public class ApplyToJobCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRepository<Candidate>> _candidateRepo = new();
    private readonly Mock<IRepository<Job>> _jobRepo = new();
    private readonly Mock<IRepository<ATS.Domain.Entities.Application>> _applicationRepo = new();
    private readonly Mock<IAiService> _aiService = new();
    private readonly Mock<INotificationService> _notifications = new();

    private ApplyToJobCommandHandler CreateHandler() => new(_uow.Object, _aiService.Object, _notifications.Object);

    [Fact]
    public async Task Handle_DuplicateApplication_ThrowsConflict()
    {
        var candidateUserId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        var candidate = new Candidate
        {
            UserId = candidateUserId,
            User = new User { FirstName = "Jane", LastName = "Doe", Email = "jane@ats.local" }
        };
        var job = new Job { Id = jobId, Title = "Backend Engineer", Description = "..." };

        _candidateRepo.Setup(r => r.Query()).Returns(new[] { candidate }.AsAsyncQueryable());
        _jobRepo.Setup(r => r.Query()).Returns(new[] { job }.AsAsyncQueryable());
        _applicationRepo.Setup(r => r.ExistsAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ATS.Domain.Entities.Application, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _uow.Setup(u => u.Repository<Candidate>()).Returns(_candidateRepo.Object);
        _uow.Setup(u => u.Repository<Job>()).Returns(_jobRepo.Object);
        _uow.Setup(u => u.Repository<ATS.Domain.Entities.Application>()).Returns(_applicationRepo.Object);

        var act = async () => await CreateHandler().Handle(new ApplyToJobCommand(candidateUserId, jobId), default);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*already applied*");
    }

    [Fact]
    public async Task Handle_NewApplication_ComputesAiMatchScoreAndSaves()
    {
        var candidateUserId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        var candidate = new Candidate
        {
            UserId = candidateUserId,
            User = new User { FirstName = "Jane", LastName = "Doe", Email = "jane@ats.local" }
        };
        var job = new Job { Id = jobId, Title = "Backend Engineer", Description = "Build APIs", CreatedByRecruiterId = Guid.NewGuid() };

        _candidateRepo.Setup(r => r.Query()).Returns(new[] { candidate }.AsAsyncQueryable());
        _jobRepo.Setup(r => r.Query()).Returns(new[] { job }.AsAsyncQueryable());
        _applicationRepo.Setup(r => r.ExistsAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ATS.Domain.Entities.Application, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _aiService.Setup(a => a.ComputeMatchScoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MatchScoreResult(87, new List<string>(), new List<string>(), "Strong fit", "Recommend interview"));

        _uow.Setup(u => u.Repository<Candidate>()).Returns(_candidateRepo.Object);
        _uow.Setup(u => u.Repository<Job>()).Returns(_jobRepo.Object);
        _uow.Setup(u => u.Repository<ATS.Domain.Entities.Application>()).Returns(_applicationRepo.Object);

        var result = await CreateHandler().Handle(new ApplyToJobCommand(candidateUserId, jobId), default);

        result.MatchScore.Should().Be(87);
        result.JobTitle.Should().Be("Backend Engineer");
        _applicationRepo.Verify(r => r.AddAsync(It.IsAny<ATS.Domain.Entities.Application>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
