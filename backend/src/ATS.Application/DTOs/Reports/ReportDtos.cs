namespace ATS.Application.DTOs.Reports;

public record HiringReportRow(
    string JobTitle,
    string Department,
    int TotalApplications,
    int Shortlisted,
    int Interviewed,
    int Offered,
    int Hired,
    double? AvgDaysToHire);

public record RecruiterPerformanceRow(
    string RecruiterName,
    int JobsPosted,
    int ApplicationsReceived,
    int InterviewsScheduled,
    int OffersExtended,
    int Hires);

public record CandidateReportRow(
    string CandidateName,
    string Email,
    int ApplicationsSubmitted,
    double? AvgMatchScore,
    string LatestStatus);

public record DepartmentReportRow(
    string Department,
    int OpenJobs,
    int TotalApplications,
    int Hired);

public record JobReportRow(
    string JobTitle,
    string Status,
    DateTime CreatedAtUtc,
    int Applications,
    int Hired);

// Generic wrapper so a single export endpoint can handle any report type.
public record ReportResult<T>(string Title, IReadOnlyList<T> Rows, DateTime GeneratedAtUtc);
