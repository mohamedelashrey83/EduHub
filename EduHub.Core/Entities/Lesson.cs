namespace EduHub.Core.Entities;

public class Lesson
{
    public int Id { get; set; }
    public int SectionId { get; set; }
    public virtual CourseSection? Section { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? VideoUrl { get; set; }
    public int DurationSeconds { get; set; }
    public int DurationMinutes => (int)Math.Ceiling(DurationSeconds / 60.0);
    public int OrderIndex { get; set; }
    public bool IsFreePreview { get; set; }
    public string? ContentNotes { get; set; }
    public string? ResourceFileUrl { get; set; }

    // Navigation properties
    public virtual ICollection<LessonCompletion> LessonCompletions { get; set; } = new List<LessonCompletion>();
    public virtual ICollection<LessonNote> LessonNotes { get; set; } = new List<LessonNote>();
    public virtual ICollection<LessonBookmark> Bookmarks { get; set; } = new List<LessonBookmark>();
}
