using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Domain.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace PersonalAssistant.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<JournalEntry> JournalEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OriginalText).IsRequired(false);
            entity.Property(e => e.TelegramFileId).IsRequired(false);
            entity.Property(e => e.LocalFilePath).IsRequired(false);
            entity.Property(e => e.TranscribedText).IsRequired(false);
        });
    }
}