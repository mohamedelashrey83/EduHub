namespace EduHub.Core.Entities;

public class Enrollment
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public virtual ApplicationUser? Student { get; set; }

    public int CourseId { get; set; }
    public virtual Course? Course { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public double ProgressPercentage { get; set; } = 0.0;
    public bool IsCompleted { get; set; }

    // Navigation properties
    public virtual Certificate? Certificate { get; set; }
    public virtual ICollection<LessonCompletion> LessonCompletions { get; set; } = new List<LessonCompletion>();
}
