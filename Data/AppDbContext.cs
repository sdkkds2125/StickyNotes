using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StickyNotes.Models;

namespace StickyNotes.Data;

public class AppDbContext : IdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Note> Notes => Set<Note>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Note>(entity =>
        {
            entity.HasIndex(n => n.UserId);
            entity.HasMany(n => n.ChecklistItems)
                  .WithOne(c => c.Note)
                  .HasForeignKey(c => c.NoteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ChecklistItem>(entity =>
        {
            entity.HasIndex(c => c.NoteId);
        });
    }
}
