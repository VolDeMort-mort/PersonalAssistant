using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations;

public class FinanceTransactionConfiguration : IEntityTypeConfiguration<FinanceTransaction>
{
    public void Configure(EntityTypeBuilder<FinanceTransaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Comment)
            .HasMaxLength(FinanceTransaction.MaxCommentLength);

        // A category that is in use can't be deleted: history must keep its meaning
        builder.HasOne<FinanceCategory>()
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Deleting a scheduled payment keeps what was already paid, just without the link
        builder.HasOne<ScheduledPayment>()
            .WithMany()
            .HasForeignKey(t => t.ScheduledPaymentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Monthly totals filter by chat and time range
        builder.HasIndex(t => new { t.ChatId, t.CreatedAt });
    }
}
