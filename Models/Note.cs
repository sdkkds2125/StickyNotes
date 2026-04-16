using System.ComponentModel.DataAnnotations;

namespace StickyNotes.Models;

public enum NoteType
{
    Text,
    Checklist
}

public class Note
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    [MaxLength(7)]
    public string Color { get; set; } = "#fef68a";

    public NoteType Type { get; set; } = NoteType.Text;

    public bool IsPinned { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ChecklistItem> ChecklistItems { get; set; } = new();
}
