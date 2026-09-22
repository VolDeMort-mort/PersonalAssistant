using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Domain.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace PersonalAssistant.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<JournalEntry> JournalEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Налаштовуємо таблицю
        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).IsRequired();
            entity.Property(e => e.EmotionTag).HasMaxLength(50);
        });

        base.OnModelCreating(modelBuilder);
    }
}