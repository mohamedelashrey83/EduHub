using EduHub.Core.Entities;

namespace EduHub.Services.Interfaces;

public class CourseReviewSummaryDto
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();
}

public interface IReviewService
{
    Task<CourseReviewSummaryDto> GetCourseReviewsSummaryAsync(int courseId);
    Task<Review> AddOrUpdateReviewAsync(string studentId, int courseId, int rating, string comment);
    Task<int> VoteHelpfulAsync(int reviewId, string userId);
    Task<bool> CanStudentReviewCourseAsync(string studentId, int courseId);
}
