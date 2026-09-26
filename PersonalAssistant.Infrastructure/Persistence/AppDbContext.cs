using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Infrastructure.Persistence;

// DbContext already tracks changes and saves them in one transaction, so it is the Unit of Work itself
public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<JournalEntry> JournalEntries { get; set; }
    public DbSet<FinanceTransaction> FinanceTransactions { get; set; }
    public DbSet<FinanceCategory> FinanceCategories { get; set; }
    public DbSet<FinanceTemplate> FinanceTemplates { get; set; }
    public DbSet<ScheduledPayment> ScheduledPayments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration in Persistence/Configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

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