using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ATS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notify;

    public NotesController(IUnitOfWork uow, ICurrentUserService currentUser, INotificationService notify)
    {
        _uow         = uow;
        _currentUser = currentUser;
        _notify      = notify;
    }

    // GET /api/notes?applicationId=
    [HttpGet]
    public async Task<IActionResult> GetNotes([FromQuery] Guid applicationId, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var role   = _currentUser.Role ?? string.Empty;

        var query = _uow.Repository<CandidateNote>().Query()
            .Include(n => n.Author)
            .Include(n => n.Mentions).ThenInclude(m => m.MentionedUser)
            .Where(n => n.ApplicationId == applicationId);

        // Filter by visibility
        if (role is not ("HRManager" or "SuperAdmin"))
        {
            query = query.Where(n =>
                n.Visibility == NoteVisibility.Public ||
                (n.Visibility == NoteVisibility.HiringManagerOnly && role == "Recruiter") ||
                (n.Visibility == NoteVisibility.Private && n.AuthorId == userId));
        }

        var notes = await query
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.CreatedAtUtc)
            .Select(n => new
            {
                n.Id, n.Content, n.Visibility, n.IsPinned, n.CreatedAtUtc,
                Author = new { n.Author.Id, Name = n.Author.FirstName + " " + n.Author.LastName },
                Mentions = n.Mentions.Select(m => new { m.MentionedUserId, Name = m.MentionedUser.FirstName + " " + m.MentionedUser.LastName })
            })
            .ToListAsync(ct);

        return Ok(notes);
    }

    // POST /api/notes
    [HttpPost]
    public async Task<IActionResult> CreateNote([FromBody] CreateNoteRequest req, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        var note = new CandidateNote
        {
            Id            = Guid.NewGuid(),
            ApplicationId = req.ApplicationId,
            AuthorId      = userId,
            Content       = req.Content,
            Visibility    = req.Visibility,
        };

        // Process @mentions
        foreach (var mentionedId in req.MentionedUserIds ?? new())
        {
            note.Mentions.Add(new NoteMention
            {
                Id              = Guid.NewGuid(),
                MentionedUserId = mentionedId
            });
        }

        await _uow.Repository<CandidateNote>().AddAsync(note, ct);
        await _uow.SaveChangesAsync(ct);

        // Notify @mentioned users (real-time via SignalR)
        foreach (var mentionedId in req.MentionedUserIds ?? new())
        {
            await _notify.NotifyUserAsync(mentionedId,
                "You were mentioned in a note",
                req.Content.Length > 80 ? req.Content[..80] + "..." : req.Content,
                ct);
        }

        return Ok(new { note.Id });
    }

    // DELETE /api/notes/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteNote(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var note = await _uow.Repository<CandidateNote>().Query()
            .FirstOrDefaultAsync(n => n.Id == id, ct);
        if (note is null) return NotFound();
        if (note.AuthorId != userId && _currentUser.Role is not ("SuperAdmin" or "HRManager"))
            return Forbid();

        _uow.Repository<CandidateNote>().Remove(note);
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }

    // PATCH /api/notes/{id}/pin
    [HttpPatch("{id:guid}/pin")]
    public async Task<IActionResult> TogglePin(Guid id, CancellationToken ct)
    {
        var note = await _uow.Repository<CandidateNote>().Query()
            .FirstOrDefaultAsync(n => n.Id == id, ct);
        if (note is null) return NotFound();

        note.IsPinned = !note.IsPinned;
        _uow.Repository<CandidateNote>().Update(note);
        await _uow.SaveChangesAsync(ct);
        return Ok(new { note.IsPinned });
    }
}

public record CreateNoteRequest(
    Guid ApplicationId,
    string Content,
    NoteVisibility Visibility,
    List<Guid>? MentionedUserIds);
