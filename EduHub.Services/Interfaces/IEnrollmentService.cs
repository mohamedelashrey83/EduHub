using EduHub.Core.Entities;

namespace EduHub.Services.Interfaces;

public class LearningDashboardDto
{
    public Enrollment Enrollment { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public Lesson CurrentLesson { get; set; } = null!;
    public List<int> CompletedLessonIds { get; set; } = new();
    public List<LessonNote> Notes { get; set; } = new();
    public List<LessonBookmark> Bookmarks { get; set; } = new();
    public int LastWatchPositionSeconds { get; set; }
}

public interface IEnrollmentService
{
    Task<IEnumerable<Enrollment>> GetStudentCoursesAsync(string studentId);
    Task<LearningDashboardDto?> GetLearningDashboardAsync(string studentId, int courseId, int? specificLessonId = null);
    Task<double> MarkLessonCompleteAsync(string studentId, int lessonId, bool isCompleted);
    Task UpdateWatchPositionAsync(string studentId, int lessonId, int seconds);
    Task<LessonNote> AddNoteAsync(string studentId, int lessonId, int timestampSeconds, string content);
    Task<bool> DeleteNoteAsync(int noteId, string studentId);
    Task<bool> ToggleBookmarkAsync(string studentId, int lessonId, string? note);
    Task<IEnumerable<LessonNote>> GetNotesForLessonAsync(string studentId, int lessonId);
}
