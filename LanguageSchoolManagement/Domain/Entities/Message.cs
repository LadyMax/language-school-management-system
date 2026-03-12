namespace LanguageSchoolManagement.Domain.Entities;

public class Message
{
    public Guid Id { get; set; }

    public Guid SenderUserId { get; set; }

    public Guid ReceiverUserId { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? AiSuggestedCategory { get; set; }

    public double? AiConfidence { get; set; }

    public string? FinalCategory { get; set; }

    public Guid? ReviewedByAdminId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}