using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StickyNotes.Data;
using StickyNotes.Models;

namespace StickyNotes.Pages;

[Authorize]
public class NoteEditorModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public NoteEditorModel(AppDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty]
    public int NoteId { get; set; }

    [BindProperty]
    public string Title { get; set; } = string.Empty;

    [BindProperty]
    public new string Content { get; set; } = string.Empty;

    [BindProperty]
    public string Color { get; set; } = "#fef68a";

    [BindProperty]
    public string NoteType { get; set; } = "Text";

    [BindProperty]
    public bool IsPinned { get; set; }

    [BindProperty]
    public List<ChecklistItemInput> ChecklistItems { get; set; } = new();

    public class ChecklistItemInput
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsChecked { get; set; }
        public int Position { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return Page(); // New note

        var userId = _userManager.GetUserId(User);
        var note = await _db.Notes
            .Include(n => n.ChecklistItems.OrderBy(c => c.Position))
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (note == null) return RedirectToPage("/Index");

        NoteId = note.Id;
        Title = note.Title;
        Content = note.Content;
        Color = note.Color;
        NoteType = note.Type.ToString();
        IsPinned = note.IsPinned;
        ChecklistItems = note.ChecklistItems.Select(c => new ChecklistItemInput
        {
            Id = c.Id,
            Content = c.Content,
            IsChecked = c.IsChecked,
            Position = c.Position
        }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return RedirectToPage("/Account/Login");

        var noteType = NoteType == "Checklist" ? Models.NoteType.Checklist : Models.NoteType.Text;

        if (NoteId == 0)
        {
            // Create new note
            var note = new Note
            {
                UserId = userId,
                Title = Title?.Trim() ?? string.Empty,
                Content = Content?.Trim() ?? string.Empty,
                Color = Color ?? "#fef68a",
                Type = noteType,
                IsPinned = IsPinned,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (noteType == Models.NoteType.Checklist)
            {
                note.ChecklistItems = ChecklistItems
                    .Where(c => !string.IsNullOrWhiteSpace(c.Content))
                    .Select((c, i) => new ChecklistItem
                    {
                        Content = c.Content.Trim(),
                        IsChecked = c.IsChecked,
                        Position = i
                    }).ToList();
            }

            _db.Notes.Add(note);
            await _db.SaveChangesAsync();
        }
        else
        {
            // Update existing note
            var note = await _db.Notes
                .Include(n => n.ChecklistItems)
                .FirstOrDefaultAsync(n => n.Id == NoteId && n.UserId == userId);

            if (note == null) return RedirectToPage("/Index");

            note.Title = Title?.Trim() ?? string.Empty;
            note.Content = Content?.Trim() ?? string.Empty;
            note.Color = Color ?? "#fef68a";
            note.Type = noteType;
            note.IsPinned = IsPinned;
            note.UpdatedAt = DateTime.UtcNow;

            // Replace checklist items
            _db.ChecklistItems.RemoveRange(note.ChecklistItems);

            if (noteType == Models.NoteType.Checklist)
            {
                note.ChecklistItems = ChecklistItems
                    .Where(c => !string.IsNullOrWhiteSpace(c.Content))
                    .Select((c, i) => new ChecklistItem
                    {
                        Content = c.Content.Trim(),
                        IsChecked = c.IsChecked,
                        Position = i
                    }).ToList();
            }

            await _db.SaveChangesAsync();
        }

        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = _userManager.GetUserId(User);
        var note = await _db.Notes
            .Include(n => n.ChecklistItems)
            .FirstOrDefaultAsync(n => n.Id == NoteId && n.UserId == userId);

        if (note != null)
        {
            _db.Notes.Remove(note);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("/Index");
    }
}
