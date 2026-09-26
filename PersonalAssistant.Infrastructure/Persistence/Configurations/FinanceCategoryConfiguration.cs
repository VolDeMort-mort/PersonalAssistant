using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations;

public class FinanceCategoryConfiguration : IEntityTypeConfiguration<FinanceCategory>
{
    public void Configure(EntityTypeBuilder<FinanceCategory> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(FinanceCategory.MaxNameLength)
            .IsRequired();

        // Two categories with the same name would be indistinguishable on buttons
        builder.HasIndex(c => new { c.ChatId, c.Type, c.Name }).IsUnique();
    }
}
