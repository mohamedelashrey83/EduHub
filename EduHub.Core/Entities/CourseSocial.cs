namespace EduHub.Core.Entities;

public class CourseAnnouncement
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public virtual Course? Course { get; set; }

    public string InstructorId { get; set; } = string.Empty;
    public virtual ApplicationUser? Instructor { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CourseQuestion
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public virtual Course? Course { get; set; }

    public int? LessonId { get; set; }
    public virtual Lesson? Lesson { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public virtual ApplicationUser? Student { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<CourseAnswer> Answers { get; set; } = new List<CourseAnswer>();
}

public class CourseAnswer
{
    public int Id { get; set; }
    public int CourseQuestionId { get; set; }
    public virtual CourseQuestion? Question { get; set; }

    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser? User { get; set; }

    public string Content { get; set; } = string.Empty;
    public bool IsInstructorReply { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser? User { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Type { get; set; } = "General"; // "Enrollment", "Certificate", "Announcement", "Q&A"
}
