using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StickyNotes.Data;
using StickyNotes.Models;

namespace StickyNotes.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<Note> Notes { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return RedirectToPage("/Account/Login");

        Notes = await _db.Notes
            .Where(n => n.UserId == userId)
            .Include(n => n.ChecklistItems)
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.UpdatedAt)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostTogglePinAsync(int noteId)
    {
        var userId = _userManager.GetUserId(User);
        var note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == noteId && n.UserId == userId);
        
        if (note != null)
        {
            note.IsPinned = !note.IsPinned;
            note.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int noteId)
    {
        var userId = _userManager.GetUserId(User);
        var note = await _db.Notes
            .Include(n => n.ChecklistItems)
            .FirstOrDefaultAsync(n => n.Id == noteId && n.UserId == userId);
        
        if (note != null)
        {
            _db.Notes.Remove(note);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}
