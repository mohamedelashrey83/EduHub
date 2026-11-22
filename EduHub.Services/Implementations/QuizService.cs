using EduHub.Core.Entities;
using EduHub.Core.Enums;
using EduHub.Core.Interfaces;
using EduHub.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EduHub.Services.Implementations;

public class QuizService : IQuizService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<QuizService> _logger;

    public QuizService(IUnitOfWork unitOfWork, ILogger<QuizService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Quiz?> GetQuizByIdAsync(int quizId)
    {
        var quiz = await _unitOfWork.Quizzes.GetByIdAsync(quizId);
        if (quiz == null) return null;

        var questions = await _unitOfWork.QuizQuestions.FindAsync(q => q.QuizId == quizId);
        foreach (var q in questions)
        {
            var answers = await _unitOfWork.QuizAnswers.FindAsync(a => a.QuestionId == q.Id);
            q.Answers = answers.ToList();
        }
        quiz.Questions = questions.OrderBy(q => q.OrderIndex).ToList();
        return quiz;
    }

    public async Task<QuizEvaluationResultDto> SubmitQuizAttemptAsync(
        string studentId,
        int quizId,
        Dictionary<int, string> userAnswers)
    {
        var quiz = await GetQuizByIdAsync(quizId);
        if (quiz == null) throw new InvalidOperationException("Quiz not found.");

        int totalPoints = 0;
        int earnedPoints = 0;
        var questionResults = new List<QuestionResultDto>();

        foreach (var q in quiz.Questions)
        {
            totalPoints += q.Points;
            userAnswers.TryGetValue(q.Id, out var studentAns);
            studentAns = studentAns?.Trim() ?? string.Empty;

            bool isCorrect = false;
            string correctAnsText = "";

            if (q.QuestionType == QuestionType.MultipleChoice || q.QuestionType == QuestionType.TrueFalse)
            {
                var correct = q.Answers.FirstOrDefault(a => a.IsCorrect);
                correctAnsText = correct?.AnswerText ?? "";
                if (!string.IsNullOrEmpty(correctAnsText) &&
                    string.Equals(studentAns, correctAnsText, StringComparison.OrdinalIgnoreCase))
                {
                    isCorrect = true;
                }
            }
            else // Fill In Blank
            {
                var correctAnswers = q.Answers.Where(a => a.IsCorrect).Select(a => a.AnswerText.Trim()).ToList();
                correctAnsText = string.Join(" OR ", correctAnswers);
                if (correctAnswers.Any(c => string.Equals(c, studentAns, StringComparison.OrdinalIgnoreCase)))
                {
                    isCorrect = true;
                }
            }

            if (isCorrect)
            {
                earnedPoints += q.Points;
            }

            questionResults.Add(new QuestionResultDto
            {
                QuestionId = q.Id,
                QuestionText = q.QuestionText,
                StudentAnswer = studentAns,
                CorrectAnswer = correctAnsText,
                IsCorrect = isCorrect,
                Explanation = q.Explanation
            });
        }

        int scorePercentage = totalPoints > 0 ? (int)Math.Round(((double)earnedPoints / totalPoints) * 100.0) : 0;
        bool isPassed = scorePercentage >= quiz.PassingScorePercentage;

        var attempt = new QuizAttempt
        {
            QuizId = quizId,
            StudentId = studentId,
            ScorePercentage = scorePercentage,
            IsPassed = isPassed,
            AttemptedAt = DateTime.UtcNow
        };

        await _unitOfWork.QuizAttempts.AddAsync(attempt);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Student {Student} took quiz #{QuizId}: Score={Score}%, Passed={Passed}",
            studentId, quizId, scorePercentage, isPassed);

        return new QuizEvaluationResultDto
        {
            QuizId = quizId,
            QuizTitle = quiz.Title,
            ScorePercentage = scorePercentage,
            PassingScorePercentage = quiz.PassingScorePercentage,
            IsPassed = isPassed,
            EarnedPoints = earnedPoints,
            TotalPoints = totalPoints,
            QuestionResults = questionResults
        };
    }

    public async Task<IEnumerable<QuizAttempt>> GetStudentQuizAttemptsAsync(string studentId, int quizId)
    {
        var attempts = await _unitOfWork.QuizAttempts.FindAsync(a => a.StudentId == studentId && a.QuizId == quizId);
        return attempts.OrderByDescending(a => a.AttemptedAt);
    }

    public async Task<Quiz> CreateQuizAsync(Quiz quiz)
    {
        await _unitOfWork.Quizzes.AddAsync(quiz);
        await _unitOfWork.SaveChangesAsync();
        return quiz;
    }
}
