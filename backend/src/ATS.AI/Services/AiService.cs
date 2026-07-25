using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using ATS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace ATS.AI.Services;

/// <summary>
/// Gemini-backed implementation of IAiService.
/// Uses the Gemini generateContent REST API (v1beta).
/// Configure via appsettings: Ai:ApiKey, Ai:Model, Ai:BaseUrl.
/// </summary>
public class AiService : IAiService
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _apiKey;
    private readonly ILogger<AiService> _logger;

    // Gemini generates structured JSON much more reliably with explicit schema instructions.
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AiService(HttpClient http, IConfiguration config, ILogger<AiService> logger)
    {
        _http = http;
        _logger = logger;

        var baseUrl = config["Ai:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/";
        _http.BaseAddress = new Uri(baseUrl);

        _apiKey = config["Ai:ApiKey"] ?? string.Empty;
        _model = config["Ai:Model"] ?? "gemini-2.5-flash";
    }

    /// <inheritdoc/>
    public async Task<string> GenerateJobDescriptionAsync(
        string title,
        string department,
        string experienceLevel,
        string keySkills,
        CancellationToken ct = default)
    {
        var prompt =
            $"Write a professional job description for a {title} role in the {department} department, " +
            $"requiring {experienceLevel} experience and skills in {keySkills}. " +
            "Include a 3-paragraph summary, detailed responsibilities (8-10 bullet points), " +
            "and specific requirements (skills, education, nice-to-haves). " +
            "Use a professional but engaging tone suitable for a top-tier company.";

        try
        {
            var result = await CompleteAsync(prompt, ct);
            if (!string.IsNullOrWhiteSpace(result) && !result.StartsWith("[AI service not configured"))
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini API failed for GenerateJobDescriptionAsync; utilizing smart template fallback.");
        }

        return GenerateFallbackJobDescription(title, department, experienceLevel, keySkills);
    }

    /// <inheritdoc/>
    public async Task<ResumeParseResult> ParseResumeAsync(
        Stream resumeStream,
        string fileName,
        CancellationToken ct = default)
    {
        string text = string.Empty;

        // Try using PdfPig first for PDF files
        if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var pdf = PdfDocument.Open(resumeStream);
                var pages = new List<string>();
                foreach (var page in pdf.GetPages())
                {
                    pages.Add(page.Text);
                }
                text = string.Join("\n", pages);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse PDF resume {FileName} using PdfPig, falling back to basic text read", fileName);
            }
        }

        // If not PDF or parsing failed/returned empty, read as plain text
        if (string.IsNullOrWhiteSpace(text))
        {
            try
            {
                // Reset stream position if possible, in case previous read advanced it
                if (resumeStream.CanSeek)
                {
                    resumeStream.Position = 0;
                }
                using var reader = new StreamReader(resumeStream, leaveOpen: true);
                text = await reader.ReadToEndAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read resume stream for {FileName}; using empty text", fileName);
            }
        }

        // Trim to avoid exceeding Gemini context limits
        if (text.Length > 15_000)
            text = text[..15_000];

        var prompt =
            "You are a professional resume parser. Extract structured information from the following resume text. " +
            "Return ONLY a valid JSON object (no markdown, no explanation) with exactly this structure:\n" +
            "{\"skills\":[\"string\"],\"missingFields\":[\"string\"],\"summary\":\"string\"," +
            "\"experience\":[\"string\"],\"education\":[\"string\"],\"certifications\":[\"string\"]}\n\n" +
            "Resume text:\n" + text;

        var json = await CompleteAsync(prompt, ct);
        json = ExtractJson(json);

        try
        {
            return JsonSerializer.Deserialize<ResumeParseResult>(json, JsonOpts)
                   ?? EmptyResumeParseResult();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize ResumeParseResult; raw JSON: {Json}", json);
            return EmptyResumeParseResult();
        }
    }

    /// <inheritdoc/>
    public async Task<MatchScoreResult> ComputeMatchScoreAsync(
        string resumeText,
        string jobDescription,
        CancellationToken ct = default)
    {
        var prompt =
            "You are an expert recruiter AI. Compare the candidate resume to the job description and return ONLY a valid JSON object (no markdown, no explanation):\n" +
            "{\"score\":85,\"missingSkills\":[\"string\"],\"recommendedSkills\":[\"string\"]," +
            "\"experienceFit\":\"string\",\"overallRecommendation\":\"string\"}\n\n" +
            $"score must be 0-100 integer. missingSkills = skills in job not in resume. recommendedSkills = extra skills candidate should learn.\n\n" +
            $"RESUME:\n{resumeText}\n\nJOB DESCRIPTION:\n{jobDescription}";

        var json = await CompleteAsync(prompt, ct);
        json = ExtractJson(json);

        try
        {
            return JsonSerializer.Deserialize<MatchScoreResult>(json, JsonOpts)
                   ?? EmptyMatchScoreResult();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize MatchScoreResult; raw JSON: {Json}", json);
            return EmptyMatchScoreResult();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GenerateInterviewQuestionsAsync(
        string jobTitle,
        string resumeText,
        string experienceLevel,
        CancellationToken ct = default)
    {
        var prompt =
            $"You are an expert technical interviewer. Generate 10 tailored interview questions for a {jobTitle} candidate at {experienceLevel} level. " +
            $"Mix of technical, behavioral, and situational questions based on the resume. " +
            $"Return ONLY a valid JSON array of strings (no markdown, no explanation): [\"question1\",\"question2\",...]\n\n" +
            $"Resume:\n{resumeText}";

        var json = await CompleteAsync(prompt, ct);
        json = ExtractJson(json);

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOpts)
                   ?? new List<string>();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize interview questions; raw JSON: {Json}", json);
            return new List<string>();
        }
    }

    /// <inheritdoc/>
    public async Task<string> GenerateCandidateSummaryAsync(
        string resumeText,
        CancellationToken ct = default)
    {
        return await CompleteAsync(
            $"You are a senior recruiter. Write a concise, professional 3-4 sentence summary of this candidate for another recruiter. " +
            $"Focus on years of experience, key strengths, domain expertise, and what makes them stand out. " +
            $"Be specific and avoid generic phrases.\n\nResume:\n{resumeText}",
            ct);
    }

    /// <inheritdoc/>
    public async Task<string> GenerateEmailAsync(
        string purpose,
        string context,
        CancellationToken ct = default)
    {
        return await CompleteAsync(
            $"Write a professional, warm, and concise recruitment email for the following purpose: {purpose}. " +
            $"Context: {context}. Keep it under 200 words. Use a friendly professional tone.",
            ct);
    }

    /// <inheritdoc/>
    public async Task<string> ChatAsync(
        string prompt,
        string contextJson,
        CancellationToken ct = default)
    {
        var systemContext =
            "You are an AI Hiring Copilot for a recruitment platform. " +
            "You have access to real-time data about jobs and candidates in the system. " +
            "Answer questions concisely and helpfully based on the provided context. " +
            "If asked to find candidates, search the context for matches. " +
            "If the answer is not in the context, say so honestly.";

        return await CompleteAsync(
            $"{systemContext}\n\nCurrent pipeline data:\n{contextJson}\n\nRecruiter question: {prompt}",
            ct);
    }

    /// <inheritdoc/>
    public async Task<SkillGapResult> AnalyzeSkillGapAsync(
        string resumeText,
        string jobDescription,
        string jobTitle,
        CancellationToken ct = default)
    {
        var prompt =
            $"You are an expert technical recruiter analyzing a candidate for a {jobTitle} role. " +
            "Return ONLY a valid JSON object (no markdown, no explanation):\n" +
            "{\"candidateHas\":[\"string\"],\"jobRequires\":[\"string\"]," +
            "\"gapSkills\":[\"string\"],\"bonusSkills\":[\"string\"]," +
            "\"learningRecommendations\":\"string\",\"gapSeverity\":2}\n\n" +
            "gapSeverity: 1=Minor gap (candidate is strong), 2=Moderate gap (needs some upskilling), 3=Critical gap (major mismatch).\n" +
            "candidateHas = skills in resume. jobRequires = skills in JD. gapSkills = required but missing. bonusSkills = candidate has but not required.\n\n" +
            $"JOB DESCRIPTION:\n{jobDescription}\n\nRESUME:\n{resumeText}";

        var json = await CompleteAsync(prompt, ct);
        json = ExtractJson(json);

        try
        {
            return JsonSerializer.Deserialize<SkillGapResult>(json, JsonOpts)
                   ?? new SkillGapResult(new List<string>(), new List<string>(), new List<string>(), new List<string>(), string.Empty, 2);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize SkillGapResult; raw JSON: {Json}", json);
            return new SkillGapResult(new List<string>(), new List<string>(), new List<string>(), new List<string>(), string.Empty, 2);
        }
    }

    /// <inheritdoc/>
    public async Task<CandidateComparisonResult> CompareCandidatesAsync(
        IReadOnlyList<CandidateSummaryInput> candidates,
        string jobTitle,
        string jobDescription,
        CancellationToken ct = default)
    {
        var candidatesText = string.Join("\n\n---\n\n",
            candidates.Select((c, i) => $"CANDIDATE {i + 1}: {c.Name} (Match Score: {c.MatchScore}%)\n{c.ResumeText}"));

        var prompt =
            $"You are a senior recruiter comparing {candidates.Count} candidates for a {jobTitle} role. " +
            "Return ONLY valid JSON (no markdown):\n" +
            "{\"bestCandidateName\":\"string\",\"summary\":\"string\"," +
            "\"rankings\":[{\"candidateName\":\"string\",\"rank\":1,\"strengths\":\"string\",\"weaknesses\":\"string\",\"hiringRecommendation\":\"string\"}]}\n\n" +
            $"JOB DESCRIPTION:\n{jobDescription}\n\nCANDIDATES:\n{candidatesText}";

        var json = await CompleteAsync(prompt, ct);
        json = ExtractJson(json);

        try
        {
            return JsonSerializer.Deserialize<CandidateComparisonResult>(json, JsonOpts)
                   ?? new CandidateComparisonResult(string.Empty, string.Empty, new List<CandidateRanking>());
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize CandidateComparisonResult; raw JSON: {Json}", json);
            return new CandidateComparisonResult(string.Empty, string.Empty, new List<CandidateRanking>());
        }
    }

    /// <inheritdoc/>
    public async Task<FeedbackSummaryResult> SummarizeFeedbackAsync(
        IReadOnlyList<FeedbackInput> feedbacks,
        string candidateName,
        string jobTitle,
        CancellationToken ct = default)
    {
        var feedbackText = string.Join("\n\n", feedbacks.Select((f, i) =>
            $"Interviewer {i + 1}: {f.InterviewerName}\nRating: {f.Rating}/5\nRecommend: {f.Recommend}\nStrengths: {f.Strengths}\nWeaknesses: {f.Weaknesses}\nComments: {f.Comments}"));

        var prompt =
            $"You are a hiring committee chair. Summarize all interviewer feedback for {candidateName} applying for {jobTitle}. " +
            "Return ONLY valid JSON (no markdown):\n" +
            "{\"overallRecommendation\":\"Hire\",\"summary\":\"string\"," +
            "\"keyStrengths\":[\"string\"],\"keyConcerns\":[\"string\"],\"averageRating\":4.2}\n\n" +
            "overallRecommendation must be exactly one of: 'Strong Hire', 'Hire', 'No Hire', 'Strong No Hire'.\n\n" +
            $"FEEDBACK:\n{feedbackText}";

        var json = await CompleteAsync(prompt, ct);
        json = ExtractJson(json);

        try
        {
            return JsonSerializer.Deserialize<FeedbackSummaryResult>(json, JsonOpts)
                   ?? new FeedbackSummaryResult("No Hire", string.Empty, new List<string>(), new List<string>(), 0);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize FeedbackSummaryResult; raw JSON: {Json}", json);
            return new FeedbackSummaryResult("No Hire", string.Empty, new List<string>(), new List<string>(), 0);
        }
    }

    /// <inheritdoc/>
    public async Task<string> DraftOfferLetterAsync(
        string candidateName,
        string jobTitle,
        string companyName,
        decimal offeredSalary,
        DateTime joiningDate,
        CancellationToken ct = default)
    {
        return await CompleteAsync(
            $"Draft a professional, warm, and complete offer letter for {candidateName} joining {companyName} as {jobTitle}. " +
            $"Offered salary: {offeredSalary:C0}. Joining date: {joiningDate:MMMM dd, yyyy}. " +
            "Include: congratulations opening, role details, salary, benefits summary placeholder, joining instructions, and a warm closing. " +
            "Use formal letter format with proper sections. Keep it under 400 words.",
            ct);
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private static string GenerateFallbackJobDescription(string title, string department, string experienceLevel, string keySkills)
    {
        var roleTitle = string.IsNullOrWhiteSpace(title) ? "Software Engineer" : title;
        var dept = string.IsNullOrWhiteSpace(department) ? "Engineering" : department;
        var exp = string.IsNullOrWhiteSpace(experienceLevel) ? "3+ years" : experienceLevel;
        var skills = string.IsNullOrWhiteSpace(keySkills) ? "relevant technical and domain skills" : keySkills;

        return $@"### About the Role: {roleTitle}

We are looking for an experienced and passionate **{roleTitle}** to join our **{dept}** team. You will play a critical role in building scalable systems, driving innovation, and collaborating with cross-functional teams to deliver exceptional software solutions.

### Key Responsibilities
- Architect, build, and maintain high-performance applications and services.
- Collaborate with product managers, designers, and engineering leads to define technical requirements.
- Leverage expertise in **{skills}** to write clean, modular, and well-tested code.
- Participate in code reviews, technical discussions, and architectural decision-making.
- Monitor, debug, and optimize application performance and system reliability.
- Stay updated with industry best practices and emerging technologies.

### Qualifications & Requirements
- **Experience**: {exp} of professional experience in a related role.
- **Core Skills**: Demonstrated expertise in **{skills}**.
- **Education**: Bachelor’s or Master’s degree in Computer Science, Engineering, or equivalent practical experience.
- **Soft Skills**: Strong problem-solving ability, clear communication, and a collaborative team mindset.

### What We Offer
- Competitive compensation package with annual bonuses.
- Comprehensive health, dental, and vision insurance.
- Flexible work location (Hybrid / Remote) and flexible working hours.
- Continuous learning budget for certifications, conferences, and courses.";
    }

    private async Task<string> CompleteAsync(string prompt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey.StartsWith("sk-"))
        {
            _logger.LogWarning("Gemini API key is invalid or not configured (Ai:ApiKey). Returning placeholder response.");
            return "[AI service not configured — please set a valid Google Gemini key in appsettings.json]";
        }

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.4,
                maxOutputTokens = 2048
            }
        };

        // Gemini endpoint: POST /v1beta/models/{model}:generateContent?key={apiKey}
        var endpoint = $"models/{_model}:generateContent?key={_apiKey}";

        HttpResponseMessage? response = null;
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                response = await _http.PostAsJsonAsync(endpoint, requestBody, ct);

                if (response.IsSuccessStatusCode)
                    break;

                if ((int)response.StatusCode == 429 && attempt < 3)
                {
                    // Rate limited — wait and retry
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2), ct);
                    continue;
                }

                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Gemini API error {Status} on attempt {Attempt}: {Body}",
                    response.StatusCode, attempt, errorBody);
                response.EnsureSuccessStatusCode(); // Will throw
            }
            catch (HttpRequestException ex) when (attempt < 3)
            {
                _logger.LogWarning(ex, "Gemini API request failed on attempt {Attempt}, retrying", attempt);
                await Task.Delay(TimeSpan.FromSeconds(attempt), ct);
            }
        }

        response!.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

        // Gemini response structure: candidates[0].content.parts[0].text
        return body
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }

    /// <summary>
    /// Strips markdown code fences (```json ... ```) from Gemini output when asking for JSON.
    /// Gemini often wraps JSON in fences even when instructed not to.
    /// </summary>
    private static string ExtractJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;

        var trimmed = raw.Trim();

        // Strip ```json ... ``` or ``` ... ```
        if (trimmed.StartsWith("```"))
        {
            var start = trimmed.IndexOf('\n');
            var end = trimmed.LastIndexOf("```");
            if (start >= 0 && end > start)
                return trimmed[(start + 1)..end].Trim();
        }

        return trimmed;
    }

    private static ResumeParseResult EmptyResumeParseResult() =>
        new(new List<string>(), new List<string>(), string.Empty,
            new List<string>(), new List<string>(), new List<string>());

    private static MatchScoreResult EmptyMatchScoreResult() =>
        new(0, new List<string>(), new List<string>(), string.Empty, string.Empty);
}