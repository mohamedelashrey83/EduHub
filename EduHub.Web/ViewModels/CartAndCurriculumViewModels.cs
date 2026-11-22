using EduHub.Core.Entities;
using EduHub.Core.Enums;
using EduHub.Services.Interfaces;

namespace EduHub.Web.ViewModels;

public class CartItemDto
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal EffectivePrice { get; set; }
    public CourseLevel Level { get; set; }
}

public class CartViewModel
{
    public List<CartItemDto> Items { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal FinalTotal { get; set; }
    public string? CouponCode { get; set; }
    public string? PricingStrategyApplied { get; set; }
    public string? PricingDescription { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

public class CurriculumBuilderViewModel
{
    public Course Course { get; set; } = null!;
    public List<CourseSection> Sections { get; set; } = new();
}

public class AddSectionViewModel
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

public class AddLessonViewModel
{
    public int CourseId { get; set; }
    public int SectionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? VideoUrl { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsFreePreview { get; set; }
    public string? ContentNotes { get; set; }
    public Microsoft.AspNetCore.Http.IFormFile? VideoFile { get; set; }
}

public class AddQuizViewModel
{
    public int CourseId { get; set; }
    public int SectionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PassingScorePercentage { get; set; } = 70;
    public List<AddQuizQuestionViewModel> Questions { get; set; } = new();
}

public class AddQuizQuestionViewModel
{
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; } = QuestionType.MultipleChoice;
    public string? Explanation { get; set; }
    public int Points { get; set; } = 1;
    public List<AddQuizAnswerViewModel> Answers { get; set; } = new();
}

public class AddQuizAnswerViewModel
{
    public string AnswerText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class AdminDashboardViewModel
{
    public AdminAnalyticsDto Analytics { get; set; } = null!;
    public IEnumerable<Course> PendingCourses { get; set; } = new List<Course>();
    public IEnumerable<ApplicationUser> RecentUsers { get; set; } = new List<ApplicationUser>();
    public IEnumerable<Category> Categories { get; set; } = new List<Category>();
}

public class InstructorDashboardViewModel
{
    public ApplicationUser Instructor { get; set; } = null!;
    public InstructorAnalyticsDto Analytics { get; set; } = null!;
    public IEnumerable<Course> Courses { get; set; } = new List<Course>();
}
