namespace EduHub.Core.Entities;

public class LessonNote
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public virtual ApplicationUser? Student { get; set; }

    public int LessonId { get; set; }
    public virtual Lesson? Lesson { get; set; }

    public int TimestampSeconds { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class LessonBookmark
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public virtual ApplicationUser? Student { get; set; }

    public int LessonId { get; set; }
    public virtual Lesson? Lesson { get; set; }

    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
