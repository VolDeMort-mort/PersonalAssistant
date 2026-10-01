using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations;

public class PlannerTopicConfiguration : IEntityTypeConfiguration<PlannerTopic>
{
    public void Configure(EntityTypeBuilder<PlannerTopic> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .HasMaxLength(PlannerTopic.MaxNameLength)
            .IsRequired();

        // Two topics with the same name would be indistinguishable on buttons
        builder.HasIndex(t => new { t.ChatId, t.Name }).IsUnique();
    }
}
