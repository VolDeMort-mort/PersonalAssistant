using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations;

public class ScheduledPaymentConfiguration : IEntityTypeConfiguration<ScheduledPayment>
{
    public void Configure(EntityTypeBuilder<ScheduledPayment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .HasMaxLength(ScheduledPayment.MaxTitleLength)
            .IsRequired();

        builder.HasOne<FinanceCategory>()
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // The list and the dashboard: a chat's active payments by date
        builder.HasIndex(p => new { p.ChatId, p.IsActive, p.NextDueDate });

        // The reminder worker: active payments of all chats by date
        builder.HasIndex(p => new { p.IsActive, p.NextDueDate });
    }
}
