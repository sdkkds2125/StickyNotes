using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Moq;
using StickyNotes.Data;
using StickyNotes.Models;
using StickyNotes.Pages;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Xunit;

namespace StickyNotes.Tests;

public class PageModelTests
{
    // =============================================
    // Helpers — shared setup code for all tests
    // =============================================

    private AppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private Mock<UserManager<IdentityUser>> GetMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(store.Object, null, null, null, null, null, null, null, null);
    }

    private (ClaimsPrincipal User, Mock<UserManager<IdentityUser>> MockUserManager) SetupUser(string userId)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, userId) }));
        var mockUserManager = GetMockUserManager();
        mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        return (user, mockUserManager);
    }

    private PageContext MakePageContext(ClaimsPrincipal user)
    {
        return new PageContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = user }
        };
    }

    // =============================================
    // IndexModel — Dashboard (OnGetAsync)
    // =============================================

    // Verifies that the dashboard only loads notes belonging to the currently authenticated user
    [Fact]
    public async Task IndexModel_OnGetAsync_ShouldLoadOnlyCurrentUsersNotes()
    {
        var db = GetDbContext();
        var (user, mockUserManager) = SetupUser("user1");

        db.Notes.Add(new Note { Title = "My Note", UserId = "user1", Content = "Content 1" });
        db.Notes.Add(new Note { Title = "Other User Note", UserId = "user2", Content = "Content 2" });
        await db.SaveChangesAsync();

        var model = new IndexModel(db, mockUserManager.Object) { PageContext = MakePageContext(user) };

        await model.OnGetAsync();

        Assert.Single(model.Notes);
        Assert.Equal("My Note", model.Notes.First().Title);
    }

    // Dashboard should return an empty list when the user has no notes
    [Fact]
    public async Task IndexModel_OnGetAsync_ShouldReturnEmptyListWhenNoNotes()
    {
        var db = GetDbContext();
        var (user, mockUserManager) = SetupUser("user1");

        var model = new IndexModel(db, mockUserManager.Object) { PageContext = MakePageContext(user) };

        await model.OnGetAsync();

        Assert.Empty(model.Notes);
    }

    // Pinned notes should appear before unpinned notes in the dashboard
    [Fact]
    public async Task IndexModel_OnGetAsync_ShouldSortPinnedNotesFirst()
    {
        var db = GetDbContext();
        var (user, mockUserManager) = SetupUser("user1");

        db.Notes.Add(new Note { Title = "Unpinned", UserId = "user1", IsPinned = false, UpdatedAt = DateTime.UtcNow });
        db.Notes.Add(new Note { Title = "Pinned", UserId = "user1", IsPinned = true, UpdatedAt = DateTime.UtcNow.AddMinutes(-5) });
        await db.SaveChangesAsync();

        var model = new IndexModel(db, mockUserManager.Object) { PageContext = MakePageContext(user) };

        await model.OnGetAsync();

        Assert.Equal(2, model.Notes.Count);
        Assert.Equal("Pinned", model.Notes.First().Title);
    }

    // =============================================
    // IndexModel — Toggle Pin (OnPostTogglePinAsync)
    // =============================================

    // Toggling pin should flip IsPinned from false to true
    [Fact]
    public async Task IndexModel_TogglePin_ShouldPinAnUnpinnedNote()
    {
        var db = GetDbContext();
        var (user, mockUserManager) = SetupUser("user1");

        var note = new Note { Title = "Test", UserId = "user1", IsPinned = false };
        db.Notes.Add(note);
        await db.SaveChangesAsync();

        var model = new IndexModel(db, mockUserManager.Object) { PageContext = MakePageContext(user) };

        await model.OnPostTogglePinAsync(note.Id);

        Assert.True(db.Notes.First().IsPinned);
    }

    // A user should not be able to toggle the pin on another user's note
    [Fact]
    public async Task IndexModel_TogglePin_ShouldNotAffectAnotherUsersNote()
    {
        var db = GetDbContext();
        var (user, mockUserManager) = SetupUser("user1");

        var otherNote = new Note { Title = "Not Mine", UserId = "user2", IsPinned = false };
        db.Notes.Add(otherNote);
        await db.SaveChangesAsync();

        var model = new IndexModel(db, mockUserManager.Object) { PageContext = MakePageContext(user) };

        await model.OnPostTogglePinAsync(otherNote.Id);

        Assert.False(db.Notes.First().IsPinned);
    }

    // =============================================
    // IndexModel — Delete (OnPostDeleteAsync)
    // =============================================

    // A user should not be able to delete another user's note
    [Fact]
    public async Task IndexModel_Delete_ShouldNotDeleteAnotherUsersNote()
    {
        var db = GetDbContext();
        var (user, mockUserManager) = SetupUser("user1");

        var otherNote = new Note { Title = "Not Mine", UserId = "user2" };
        db.Notes.Add(otherNote);
        await db.SaveChangesAsync();

        var model = new IndexModel(db, mockUserManager.Object) { PageContext = MakePageContext(user) };

        await model.OnPostDeleteAsync(otherNote.Id);

        Assert.Single(db.Notes);
    }

    // =============================================
    // NoteEditorModel — Load (OnGetAsync)
    // =============================================

    // Opening the editor with no ID should return a blank form (new note)
    [Fact]
    public async Task NoteEditor_OnGetAsync_ShouldReturnBlankFormForNewNote()
    {
        var db = GetDbContext();
        var (user, mockUserManager) = SetupUser("user1");

        var model = new NoteEditorModel(db, mockUserManager.Object) { PageContext = MakePageContext(user) };

        var result = await model.OnGetAsync(null);

        Assert.IsType<PageResult>(result);
        Assert.Equal(0, model.NoteId);
        Assert.Equal(string.Empty, model.Title);
    }

    // Opening the editor with a valid ID should populate the form with the note's data
    [Fact]
    public async Task NoteEditor_OnGetAsync_ShouldLoadExistingNoteData()
    {
        var db = GetDbContext();
        var (user, mockUserManager) = SetupUser("user1");

        var note = new Note
        {
            Title = "Loaded Note",
            UserId = "user1",
            Content = "Hello",
            Color = "#93c5fd",
            Type = NoteType.Text,
            IsPinned = true
        };
        db.Notes.Add(note);
        await db.SaveChangesAsync();

        var model = new NoteEditorModel(db, mockUserManager.Object) { PageContext = MakePageContext(user) };

        var result = await model.OnGetAsync(note.Id);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Loaded Note", model.Title);
        Assert.Equal("Hello", model.Content);
        Assert.Equal("#93c5fd", model.Color);
        Assert.True(model.IsPinned);
    }

    // Trying to load another user's note should redirect away instead of showing the data
    [Fact]
    public async Task NoteEditor_OnGetAsync_ShouldRedirectIfNoteDoesNotBelongToUser()
    {
        var db = GetDbContext();
        var (user, mockUserManager) = SetupUser("user1");

        var otherNote = new Note { Title = "Secret", UserId = "user2", Content = "Private data" };
        db.Notes.Add(otherNote);
        await db.SaveChangesAsync();

        var model = new NoteEditorModel(db, mockUserManager.Object) { PageContext = MakePageContext(user) };

        var result = await model.OnGetAsync(otherNote.Id);

        Assert.IsType<RedirectToPageResult>(result);
    }

    // =============================================
    // NoteEditorModel — Create (OnPostAsync)
    // =============================================

    // Submitting a new checklist should create the note with correctly assigned positions
    [Fact]
    public async Task NoteEditor_OnPostAsync_ShouldCreateNewChecklist()
    {
        var db = GetDbContext();
        var (user, mockUserManager) = SetupUser("user1");

        var model = new NoteEditorModel(db, mockUserManager.Object)
        {
            NoteId = 0,
            Title = "Checklist Note",
            NoteType = "Checklist",
            ChecklistItems = new List<NoteEditorModel.ChecklistItemInput>
            {
                new() { Content = "Item 1", Position = 1 },
                new() { Content = "Item 2", Position = 0 }
            },
            PageContext = MakePageContext(user)
        };

        var result = await model.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var note = db.Notes.Include(n => n.ChecklistItems).FirstOrDefault();
        Assert.NotNull(note);
        Assert.Equal(NoteType.Checklist, note.Type);
        Assert.Equal(2, note.ChecklistItems.Count);
    }

    // Checklist items with empty or whitespace-only content should be silently filtered out
    [Fact]
    public async Task NoteEditor_OnPostAsync_ShouldFilterOutEmptyChecklistItems()
    {
        var db = GetDbContext();
        var (user, mockUserManager) = SetupUser("user1");

        var model = new NoteEditorModel(db, mockUserManager.Object)
        {
            NoteId = 0,
            Title = "Sparse Checklist",
            NoteType = "Checklist",
            ChecklistItems = new List<NoteEditorModel.ChecklistItemInput>
            {
                new() { Content = "Valid item" },
                new() { Content = "" },
                new() { Content = "   " },
                new() { Content = "Another valid item" }
            },
            PageContext = MakePageContext(user)
        };

        await model.OnPostAsync();

        var note = db.Notes.Include(n => n.ChecklistItems).First();
        Assert.Equal(2, note.ChecklistItems.Count);
        Assert.Equal("Valid item", note.ChecklistItems[0].Content);
        Assert.Equal("Another valid item", note.ChecklistItems[1].Content);
    }

    // =============================================
    // NoteEditorModel — Update (OnPostAsync)
    // =============================================

    // Updating an existing note should change its title and content in the database
    [Fact]
    public async Task NoteEditor_OnPostAsync_ShouldUpdateExistingNote()
    {
        var db = GetDbContext();
        var (user, mockUserManager) = SetupUser("user1");

        var note = new Note { Title = "Old Title", UserId = "user1", Content = "Old content" };
        db.Notes.Add(note);
        await db.SaveChangesAsync();

        var model = new NoteEditorModel(db, mockUserManager.Object)
        {
            NoteId = note.Id,
            Title = "New Title",
            Content = "New content",
            NoteType = "Text",
            Color = "#86efac",
            PageContext = MakePageContext(user)
        };

        await model.OnPostAsync();

        var updated = db.Notes.First();
        Assert.Equal("New Title", updated.Title);
        Assert.Equal("New content", updated.Content);
        Assert.Equal("#86efac", updated.Color);
    }

    // =============================================
    // NoteEditorModel — Delete (OnPostDeleteAsync)
    // =============================================

    // Deleting a note should remove it and its checklist items from the database
    [Fact]
    public async Task NoteEditor_OnPostDeleteAsync_ShouldRemoveNoteAndChecklistItems()
    {
        var db = GetDbContext();
        var (user, mockUserManager) = SetupUser("user1");

        var note = new Note
        {
            Title = "Note to delete",
            UserId = "user1",
            ChecklistItems = new List<ChecklistItem> { new() { Content = "Task 1" } }
        };
        db.Notes.Add(note);
        await db.SaveChangesAsync();

        var model = new NoteEditorModel(db, mockUserManager.Object)
        {
            NoteId = note.Id,
            PageContext = MakePageContext(user)
        };

        var result = await model.OnPostDeleteAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Empty(db.Notes);
    }

    // A user should not be able to delete another user's note through the editor
    [Fact]
    public async Task NoteEditor_OnPostDeleteAsync_ShouldNotDeleteAnotherUsersNote()
    {
        var db = GetDbContext();
        var (user, mockUserManager) = SetupUser("user1");

        var otherNote = new Note { Title = "Not Mine", UserId = "user2" };
        db.Notes.Add(otherNote);
        await db.SaveChangesAsync();

        var model = new NoteEditorModel(db, mockUserManager.Object)
        {
            NoteId = otherNote.Id,
            PageContext = MakePageContext(user)
        };

        await model.OnPostDeleteAsync();

        Assert.Single(db.Notes);
    }
}

