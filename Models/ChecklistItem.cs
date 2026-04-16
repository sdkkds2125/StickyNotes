using System.ComponentModel.DataAnnotations;

namespace StickyNotes.Models;

public class ChecklistItem
{
    public int Id { get; set; }

    public int NoteId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Content { get; set; } = string.Empty;

    public bool IsChecked { get; set; } = false;

    public int Position { get; set; } = 0;

    public Note? Note { get; set; }
}
