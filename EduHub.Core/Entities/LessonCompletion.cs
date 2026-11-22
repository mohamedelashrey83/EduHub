namespace EduHub.Core.Entities;

public class LessonCompletion
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public virtual Enrollment? Enrollment { get; set; }

    public int LessonId { get; set; }
    public virtual Lesson? Lesson { get; set; }

    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int LastWatchPositionSeconds { get; set; }
}
