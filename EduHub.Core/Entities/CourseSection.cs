namespace EduHub.Core.Entities;

public class CourseSection
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public virtual Course? Course { get; set; }

    public string Title { get; set; } = string.Empty;
    public int OrderIndex { get; set; }

    // Navigation properties
    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
