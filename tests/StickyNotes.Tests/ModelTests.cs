using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using StickyNotes.Models;
using Xunit;

namespace StickyNotes.Tests;

public class ModelTests
{
    // =============================================
    // Helper to run validation on any model object
    // =============================================
    private static (bool IsValid, List<ValidationResult> Results) Validate(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, context, results, true);
        return (isValid, results);
    }

    // =============================================
    // Note — Default Values
    // =============================================

    // A freshly created note should default to the yellow sticky-note color
    [Fact]
    public void Note_DefaultColor_ShouldBeYellow()
    {
        var note = new Note();
        Assert.Equal("#fef68a", note.Color);
    }

    // A freshly created note should default to Text type, not Checklist
    [Fact]
    public void Note_DefaultType_ShouldBeText()
    {
        var note = new Note();
        Assert.Equal(NoteType.Text, note.Type);
    }

    // A freshly created note should not be pinned by default
    [Fact]
    public void Note_DefaultIsPinned_ShouldBeFalse()
    {
        var note = new Note();
        Assert.False(note.IsPinned);
    }

    // A freshly created note should start with an empty checklist items collection
    [Fact]
    public void Note_DefaultChecklistItems_ShouldBeEmptyList()
    {
        var note = new Note();
        Assert.NotNull(note.ChecklistItems);
        Assert.Empty(note.ChecklistItems);
    }

    // CreatedAt and UpdatedAt should be set to a recent UTC time on creation
    [Fact]
    public void Note_DefaultTimestamps_ShouldBeRecentUtc()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var note = new Note();
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(note.CreatedAt, before, after);
        Assert.InRange(note.UpdatedAt, before, after);
    }

    // =============================================
    // Note — Validation (Title)
    // =============================================

    // A title at exactly the 200-character limit should pass validation
    [Fact]
    public void Note_ShouldPassValidation_WhenTitleIsExactly200Characters()
    {
        var note = new Note { UserId = "user1", Title = new string('A', 200) };
        var (isValid, _) = Validate(note);
        Assert.True(isValid);
    }

    // A title exceeding 200 characters should fail validation
    [Fact]
    public void Note_ShouldFailValidation_WhenTitleExceeds200Characters()
    {
        var note = new Note { UserId = "user1", Title = new string('A', 201) };
        var (isValid, results) = Validate(note);
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains("Title"));
    }

    // An empty title is fine since the title field is optional
    [Fact]
    public void Note_ShouldPassValidation_WhenTitleIsEmpty()
    {
        var note = new Note { UserId = "user1", Title = "" };
        var (isValid, _) = Validate(note);
        Assert.True(isValid);
    }

    // =============================================
    // Note — Validation (Color)
    // =============================================

    // A color string longer than 7 characters should fail the MaxLength constraint
    [Fact]
    public void Note_ShouldFailValidation_WhenColorExceeds7Characters()
    {
        var note = new Note { UserId = "user1", Color = "#1234567" };
        var (isValid, results) = Validate(note);
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains("Color"));
    }

    // A valid 7-character hex color should pass validation
    [Fact]
    public void Note_ShouldPassValidation_WhenColorIsValid7CharHex()
    {
        var note = new Note { UserId = "user1", Color = "#abcdef" };
        var (isValid, _) = Validate(note);
        Assert.True(isValid);
    }

    // =============================================
    // Note — Parent-Child Relationship
    // =============================================

    // Checklist items added to a note should be accessible through its ChecklistItems collection
    [Fact]
    public void Note_CanHoldMultipleChecklistItems()
    {
        var note = new Note { UserId = "user1", Type = NoteType.Checklist };
        note.ChecklistItems.Add(new ChecklistItem { Content = "Buy milk", Position = 0 });
        note.ChecklistItems.Add(new ChecklistItem { Content = "Walk dog", Position = 1 });
        note.ChecklistItems.Add(new ChecklistItem { Content = "Code review", Position = 2 });

        Assert.Equal(3, note.ChecklistItems.Count);
        Assert.Equal("Walk dog", note.ChecklistItems[1].Content);
    }

    // =============================================
    // ChecklistItem — Default Values
    // =============================================

    // A new checklist item should default to unchecked
    [Fact]
    public void ChecklistItem_DefaultIsChecked_ShouldBeFalse()
    {
        var item = new ChecklistItem();
        Assert.False(item.IsChecked);
    }

    // A new checklist item should default to position 0
    [Fact]
    public void ChecklistItem_DefaultPosition_ShouldBeZero()
    {
        var item = new ChecklistItem();
        Assert.Equal(0, item.Position);
    }

    // =============================================
    // ChecklistItem — Validation (Content)
    // =============================================

    // A checklist item with no content should fail the Required constraint
    [Fact]
    public void ChecklistItem_ShouldFailValidation_WhenContentIsMissing()
    {
        var item = new ChecklistItem { NoteId = 1 };
        var (isValid, results) = Validate(item);
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains("Content"));
    }

    // Content at exactly the 500-character limit should pass validation
    [Fact]
    public void ChecklistItem_ShouldPassValidation_WhenContentIsExactly500Characters()
    {
        var item = new ChecklistItem { NoteId = 1, Content = new string('X', 500) };
        var (isValid, _) = Validate(item);
        Assert.True(isValid);
    }

    // Content exceeding 500 characters should fail the MaxLength constraint
    [Fact]
    public void ChecklistItem_ShouldFailValidation_WhenContentExceeds500Characters()
    {
        var item = new ChecklistItem { NoteId = 1, Content = new string('X', 501) };
        var (isValid, results) = Validate(item);
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains("Content"));
    }

    // A checklist item with valid content should pass all validation rules
    [Fact]
    public void ChecklistItem_ShouldPassValidation_WhenAllFieldsAreValid()
    {
        var item = new ChecklistItem { NoteId = 1, Content = "Do the dishes", Position = 3, IsChecked = true };
        var (isValid, _) = Validate(item);
        Assert.True(isValid);
    }

    // =============================================
    // NoteType Enum
    // =============================================

    // The NoteType enum should have exactly two values: Text and Checklist
    [Fact]
    public void NoteType_ShouldHaveExactlyTwoValues()
    {
        var values = Enum.GetValues<NoteType>();
        Assert.Equal(2, values.Length);
        Assert.Contains(NoteType.Text, values);
        Assert.Contains(NoteType.Checklist, values);
    }
}
