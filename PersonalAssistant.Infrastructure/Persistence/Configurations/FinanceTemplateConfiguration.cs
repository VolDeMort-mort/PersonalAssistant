using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations;

public class FinanceTemplateConfiguration : IEntityTypeConfiguration<FinanceTemplate>
{
    public void Configure(EntityTypeBuilder<FinanceTemplate> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .HasMaxLength(FinanceTemplate.MaxTitleLength)
            .IsRequired();

        builder.HasOne<FinanceCategory>()
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.ChatId, t.Type });
    }
}
