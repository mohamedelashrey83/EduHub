namespace EduHub.Services.Interfaces;

public class AdminAnalyticsDto
{
    public int TotalStudents { get; set; }
    public int TotalInstructors { get; set; }
    public int TotalCourses { get; set; }
    public int PendingApprovalCourses { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalInstructorPayouts { get; set; }
    public double AverageCompletionRate { get; set; }

    // Chart.js datasets
    public List<string> MonthlyLabels { get; set; } = new();
    public List<decimal> MonthlyRevenueData { get; set; } = new();

    public List<string> BestSellingCourseTitles { get; set; } = new();
    public List<int> BestSellingCourseEnrollments { get; set; } = new();

    public List<string> CompletionCourseTitles { get; set; } = new();
    public List<double> CompletionRates { get; set; } = new();

    public List<string> CategoryLabels { get; set; } = new();
    public List<int> CategoryCourseCounts { get; set; } = new();
}

public class InstructorAnalyticsDto
{
    public int TotalCourses { get; set; }
    public int TotalStudents { get; set; }
    public decimal TotalEarnings { get; set; }
    public double AverageRating { get; set; }

    public List<string> CourseTitles { get; set; } = new();
    public List<int> CourseEnrollments { get; set; } = new();
    public List<decimal> CourseRevenues { get; set; } = new();
}

public interface IAnalyticsService
{
    Task<AdminAnalyticsDto> GetAdminAnalyticsAsync();
    Task<InstructorAnalyticsDto> GetInstructorAnalyticsAsync(string instructorId);
}
