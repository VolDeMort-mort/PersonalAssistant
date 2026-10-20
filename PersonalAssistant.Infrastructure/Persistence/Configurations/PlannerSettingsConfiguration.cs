using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations;

public class PlannerSettingsConfiguration : IEntityTypeConfiguration<PlannerSettings>
{
    public void Configure(EntityTypeBuilder<PlannerSettings> builder)
    {
        // One row per chat; the chat id comes from Telegram, it is not generated here
        builder.HasKey(s => s.ChatId);
        builder.Property(s => s.ChatId).ValueGeneratedNever();
    }
}
