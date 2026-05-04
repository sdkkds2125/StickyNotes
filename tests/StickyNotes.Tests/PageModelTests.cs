using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

    // Verifies that the dashboard only loads notes belonging to the currently authenticated user
    [Fact]
    public async Task IndexModel_OnGetAsync_ShouldLoadNotesForUser()
    {
        var db = GetDbContext();
        var mockUserManager = GetMockUserManager();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, "user1") }));
        mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user1");

        db.Notes.Add(new Note { Title = "Test Note", UserId = "user1", Content = "Content 1" });
        db.Notes.Add(new Note { Title = "Other User Note", UserId = "user2", Content = "Content 2" });
        await db.SaveChangesAsync();

        var model = new IndexModel(db, mockUserManager.Object);
        var pageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = user }
        };
        model.PageContext = pageContext;

        await model.OnGetAsync();

        Assert.Single(model.Notes);
        Assert.Equal("Test Note", model.Notes.First().Title);
    }

    // Verifies that submitting the form successfully creates a new checklist note and assigns positions correctly
    [Fact]
    public async Task NoteEditorModel_OnPostAsync_CreatesNewChecklist()
    {
        var db = GetDbContext();
        var mockUserManager = GetMockUserManager();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, "user1") }));
        mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user1");

        var model = new NoteEditorModel(db, mockUserManager.Object)
        {
            NoteId = 0,
            Title = "Checklist Note",
            NoteType = "Checklist",
            ChecklistItems = new List<NoteEditorModel.ChecklistItemInput>
            {
                new() { Content = "Item 1", Position = 1 },
                new() { Content = "Item 2", Position = 0 }
            }
        };
        
        var pageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = user }
        };
        model.PageContext = pageContext;

        var result = await model.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var note = db.Notes.Include(n => n.ChecklistItems).FirstOrDefault();
        Assert.NotNull(note);
        Assert.Equal(NoteType.Checklist, note.Type);
        Assert.Equal(2, note.ChecklistItems.Count);
        Assert.Equal(0, note.ChecklistItems[0].Position);
        Assert.Equal("Item 1", note.ChecklistItems[0].Content);
    }

    // Verifies that a note and its associated checklist items are deleted from the database
    [Fact]
    public async Task NoteEditorModel_OnPostDeleteAsync_RemovesNoteAndChecklistItems()
    {
        var db = GetDbContext();
        var mockUserManager = GetMockUserManager();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, "user1") }));
        mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user1");

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
            NoteId = note.Id
        };
        model.PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = user }
        };

        var result = await model.OnPostDeleteAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Empty(db.Notes);
    }

    // Verifies that pinning a note toggles its IsPinned status correctly
    [Fact]
    public async Task IndexModel_OnPostTogglePinAsync_TogglesPinnedStatus()
    {
        var db = GetDbContext();
        var mockUserManager = GetMockUserManager();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, "user1") }));
        mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user1");

        var note = new Note { Title = "Unpinned Note", UserId = "user1", IsPinned = false };
        db.Notes.Add(note);
        await db.SaveChangesAsync();

        var model = new IndexModel(db, mockUserManager.Object);
        model.PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = user }
        };

        var result = await model.OnPostTogglePinAsync(note.Id);

        Assert.IsType<RedirectToPageResult>(result);
        var dbNote = db.Notes.First();
        Assert.True(dbNote.IsPinned);
    }
}
