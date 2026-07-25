using ATS.Domain.Enums;

namespace ATS.Application.DTOs.Interviews;

public record InterviewRoundDto(
    Guid Id,
    string RoundName,
    int SequenceOrder,
    IReadOnlyList<InterviewDto> Interviews);

public record InterviewDto(
    Guid Id,
    Guid InterviewRoundId,
    string RoundName,
    Guid ApplicationId,
    string JobTitle,
    string CandidateName,
    Guid InterviewerId,
    string InterviewerName,
    DateTime ScheduledAtUtc,
    int DurationMinutes,
    string? MeetingLink,
    InterviewResultStatus Result,
    FeedbackDto? Feedback);

public record FeedbackDto(
    Guid Id,
    int Rating,
    string? Strengths,
    string? Weaknesses,
    string? Comments,
    bool Recommend);

public record SubmitFeedbackDto(int Rating, string? Strengths, string? Weaknesses, string? Comments, bool Recommend, InterviewResultStatus Result);
