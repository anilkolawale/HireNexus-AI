namespace ATS.Application.DTOs.AiAssistant;

public record ChatMessageDto(string Role, string Content); // Role: "user" | "assistant"

public record ChatRequestDto(string Message, IReadOnlyList<ChatMessageDto>? History);

public record ChatResponseDto(string Reply);
