using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using StickyNotes.Models;
using Xunit;

namespace StickyNotes.Tests;

public class ModelTests
{
    [Fact]
    public void Note_ShouldHaveValidationErrors_WhenTitleIsTooLong()
    {
        var note = new Note { UserId = "user1", Title = new string('A', 201) };
        var context = new ValidationContext(note);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(note, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains("Title"));
    }

    [Fact]
    public void ChecklistItem_ShouldHaveValidationErrors_WhenContentIsMissing()
    {
        var item = new ChecklistItem { NoteId = 1 };
        var context = new ValidationContext(item);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(item, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains("Content"));
    }
}
