using System.Security.Claims;
using ATS.Application.Features.Candidates.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Candidate")]
public class ResumesController : ControllerBase
{
    private readonly IMediator _mediator;
    private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx" };
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public ResumesController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    /// <summary>Upload a resume (PDF/DOC/DOCX). Stores to Blob Storage and runs AI parsing.</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { message = "Only PDF, DOC, and DOCX files are supported." });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { message = "File exceeds the 10 MB limit." });

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        stream.Position = 0;

        var result = await _mediator.Send(
            new UploadResumeCommand(CurrentUserId, stream, file.FileName, file.ContentType), ct);

        return Ok(result);
    }
}
