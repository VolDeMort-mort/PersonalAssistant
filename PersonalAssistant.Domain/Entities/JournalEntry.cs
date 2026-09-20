using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalAssistant.Domain.Entities;

public class JournalEntry
{
    public Guid Id { get; private set; }
    public string Text { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? EmotionTag { get; private set; }

    public JournalEntry(string text, string? emotionTag = null)
    {
        Id = Guid.NewGuid();
        Text = text;
        CreatedAt = DateTime.UtcNow;
        EmotionTag = emotionTag;
    }
}