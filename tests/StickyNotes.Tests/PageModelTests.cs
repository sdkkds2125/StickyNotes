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
}
