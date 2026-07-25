namespace ATS.Application.DTOs.Dashboard;

public record RecruiterDashboardDto(
    int OpenJobs,
    int TotalApplications,
    int InterviewsThisWeek,
    int OffersExtended,
    IReadOnlyList<MonthlyCountDto> MonthlyApplications,
    IReadOnlyList<StageCountDto> PipelineByStage,
    IReadOnlyList<DepartmentCountDto> DepartmentHiring);

public record MonthlyCountDto(string Month, int Count);
public record StageCountDto(string Stage, int Count);
public record DepartmentCountDto(string Department, int OpenJobs, int Hired);

public record CandidateDashboardDto(
    int TotalApplications,
    int ActiveApplications,
    int InterviewsScheduled,
    int OffersReceived);
