using EduHub.Core.Enums;

namespace EduHub.Core.Entities;

public class Quiz
{
    public int Id { get; set; }
    public int? SectionId { get; set; }
    public virtual CourseSection? Section { get; set; }

    public int? CourseId { get; set; }
    public virtual Course? Course { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PassingScorePercentage { get; set; } = 70;

    // Navigation properties
    public virtual ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    public virtual ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
}

public class QuizQuestion
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public virtual Quiz? Quiz { get; set; }

    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; } = QuestionType.MultipleChoice;
    public string? Explanation { get; set; }
    public int Points { get; set; } = 1;
    public int OrderIndex { get; set; }

    // Navigation properties
    public virtual ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();
}

public class QuizAnswer
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public virtual QuizQuestion? Question { get; set; }

    public string AnswerText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class QuizAttempt
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public virtual Quiz? Quiz { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public virtual ApplicationUser? Student { get; set; }

    public int ScorePercentage { get; set; }
    public bool IsPassed { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public string? DetailsJson { get; set; }
}
