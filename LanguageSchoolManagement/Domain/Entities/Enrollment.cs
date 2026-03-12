namespace LanguageSchoolManagement.Domain.Entities;

public class Enrollment
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid CourseId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;

    public Course Course { get; set; } = null!;
}