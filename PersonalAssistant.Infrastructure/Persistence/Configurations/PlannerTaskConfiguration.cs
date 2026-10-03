using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations;

public class PlannerTaskConfiguration : IEntityTypeConfiguration<PlannerTask>
{
    public void Configure(EntityTypeBuilder<PlannerTask> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .HasMaxLength(PlannerTask.MaxTitleLength)
            .IsRequired();

        // A value object: its fields are columns of the task, not a table of its own
        builder.OwnsOne(t => t.Repeat, repeat =>
        {
            // Required inside a rule, so a NULL kind is read back as "no repeat"
            repeat.Property(r => r.Kind).HasColumnName("RepeatKind").IsRequired();
            repeat.Property(r => r.Interval).HasColumnName("RepeatInterval");
            repeat.Property(r => r.AnchorMonth).HasColumnName("RepeatAnchorMonth");
            repeat.Property(r => r.AnchorDay).HasColumnName("RepeatAnchorDay");
        });

        // A topic is deleted only after its tasks let go of it (see DeleteTopicCommand)
        builder.HasOne<PlannerTopic>()
            .WithMany()
            .HasForeignKey(t => t.TopicId)
            .OnDelete(DeleteBehavior.Restrict);

        // The screens: a chat's open tasks by date
        builder.HasIndex(t => new { t.ChatId, t.CompletedAt, t.DueDate });

        // The worker: open tasks of all chats by date
        builder.HasIndex(t => new { t.CompletedAt, t.DueDate });
    }
}
