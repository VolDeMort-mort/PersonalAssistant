using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations;

public class ShoppingItemConfiguration : IEntityTypeConfiguration<ShoppingItem>
{
    public void Configure(EntityTypeBuilder<ShoppingItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Title)
            .HasMaxLength(ShoppingItem.MaxTitleLength)
            .IsRequired();

        builder.Property(i => i.Link)
            .HasMaxLength(ShoppingItem.MaxLinkLength);

        // A category that is in use can't be deleted, like with transactions
        builder.HasOne<FinanceCategory>()
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // The list (not bought yet) and the history (by purchase time) of a chat
        builder.HasIndex(i => new { i.ChatId, i.BoughtAt });
    }
}
