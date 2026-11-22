namespace EduHub.Core.Patterns.Observer;

public class LessonCompletedEvent
{
    public int EnrollmentId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public double NewProgressPercentage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class CourseCompletionEvent
{
    public int EnrollmentId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string InstructorId { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}

public interface ICourseObserver
{
    string ObserverName { get; }
    Task OnLessonCompletedAsync(LessonCompletedEvent evt);
    Task OnCourseCompletedAsync(CourseCompletionEvent evt);
}

public interface ICourseProgressSubject
{
    void Attach(ICourseObserver observer);
    void Detach(ICourseObserver observer);
    Task NotifyLessonCompletedAsync(LessonCompletedEvent evt);
    Task NotifyCourseCompletedAsync(CourseCompletionEvent evt);
}
