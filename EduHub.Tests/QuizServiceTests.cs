using EduHub.Core.Entities;
using EduHub.Core.Enums;
using EduHub.Data;
using EduHub.Data.Repositories;
using EduHub.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EduHub.Tests;

public class QuizServiceTests
{
    private EduHubDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<EduHubDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new EduHubDbContext(options);
    }

    [Fact]
    public async Task SubmitQuizAttempt_AllCorrect_Returns100PercentAndPassed()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        using var uow = new UnitOfWork(context);

        var quiz = new Quiz
        {
            Title = "Architecture Quiz",
            PassingScorePercentage = 70
        };
        await uow.Quizzes.AddAsync(quiz);
        await uow.SaveChangesAsync();

        var q1 = new QuizQuestion
        {
            QuizId = quiz.Id,
            QuestionText = "Is N-Tier architecture layered?",
            QuestionType = QuestionType.TrueFalse,
            Points = 10,
            OrderIndex = 1
        };
        await uow.QuizQuestions.AddAsync(q1);
        await uow.SaveChangesAsync();

        var a1 = new QuizAnswer { QuestionId = q1.Id, AnswerText = "True", IsCorrect = true };
        var a2 = new QuizAnswer { QuestionId = q1.Id, AnswerText = "False", IsCorrect = false };
        await uow.QuizAnswers.AddAsync(a1);
        await uow.QuizAnswers.AddAsync(a2);
        await uow.SaveChangesAsync();

        var service = new QuizService(uow, NullLogger<QuizService>.Instance);
        var userAnswers = new Dictionary<int, string> { { q1.Id, "True" } };

        // Act
        var result = await service.SubmitQuizAttemptAsync("student-1", quiz.Id, userAnswers);

        // Assert
        Assert.True(result.IsPassed);
        Assert.Equal(100, result.ScorePercentage);
        Assert.Equal(10, result.EarnedPoints);
        Assert.Equal(10, result.TotalPoints);
    }

    [Fact]
    public async Task SubmitQuizAttempt_WrongAnswer_FailsWhenBelowPassingThreshold()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        using var uow = new UnitOfWork(context);

        var quiz = new Quiz
        {
            Title = "Design Patterns Quiz",
            PassingScorePercentage = 75
        };
        await uow.Quizzes.AddAsync(quiz);
        await uow.SaveChangesAsync();

        var q1 = new QuizQuestion
        {
            QuizId = quiz.Id,
            QuestionText = "Which pattern is used for pricing?",
            QuestionType = QuestionType.MultipleChoice,
            Points = 10,
            OrderIndex = 1
        };
        await uow.QuizQuestions.AddAsync(q1);
        await uow.SaveChangesAsync();

        var a1 = new QuizAnswer { QuestionId = q1.Id, AnswerText = "Strategy Pattern", IsCorrect = true };
        var a2 = new QuizAnswer { QuestionId = q1.Id, AnswerText = "Singleton Pattern", IsCorrect = false };
        await uow.QuizAnswers.AddAsync(a1);
        await uow.QuizAnswers.AddAsync(a2);
        await uow.SaveChangesAsync();

        var service = new QuizService(uow, NullLogger<QuizService>.Instance);
        var userAnswers = new Dictionary<int, string> { { q1.Id, "Singleton Pattern" } };

        // Act
        var result = await service.SubmitQuizAttemptAsync("student-1", quiz.Id, userAnswers);

        // Assert
        Assert.False(result.IsPassed);
        Assert.Equal(0, result.ScorePercentage);
        Assert.Equal(0, result.EarnedPoints);
    }
}
