using EduHub.Core.Entities;

namespace EduHub.Services.Interfaces;

public class QuizEvaluationResultDto
{
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public int ScorePercentage { get; set; }
    public int PassingScorePercentage { get; set; }
    public bool IsPassed { get; set; }
    public int EarnedPoints { get; set; }
    public int TotalPoints { get; set; }
    public List<QuestionResultDto> QuestionResults { get; set; } = new();
}

public class QuestionResultDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string StudentAnswer { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string? Explanation { get; set; }
}

public interface IQuizService
{
    Task<Quiz?> GetQuizByIdAsync(int quizId);
    Task<QuizEvaluationResultDto> SubmitQuizAttemptAsync(string studentId, int quizId, Dictionary<int, string> userAnswers);
    Task<IEnumerable<QuizAttempt>> GetStudentQuizAttemptsAsync(string studentId, int quizId);
    Task<Quiz> CreateQuizAsync(Quiz quiz);
}
