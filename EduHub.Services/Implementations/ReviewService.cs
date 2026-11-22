using EduHub.Core.Entities;
using EduHub.Core.Interfaces;
using EduHub.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EduHub.Services.Implementations;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(IUnitOfWork unitOfWork, ILogger<ReviewService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CourseReviewSummaryDto> GetCourseReviewsSummaryAsync(int courseId)
    {
        var reviews = (await _unitOfWork.Reviews.GetCourseReviewsAsync(courseId)).ToList();
        var avg = await _unitOfWork.Reviews.GetAverageRatingForCourseAsync(courseId);
        var dist = await _unitOfWork.Reviews.GetRatingDistributionAsync(courseId);

        return new CourseReviewSummaryDto
        {
            AverageRating = avg,
            TotalReviews = reviews.Count,
            RatingDistribution = dist,
            Reviews = reviews
        };
    }

    public async Task<Review> AddOrUpdateReviewAsync(string studentId, int courseId, int rating, string comment)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5 stars.");

        var existing = await _unitOfWork.Reviews.GetUserReviewForCourseAsync(studentId, courseId);
        if (existing != null)
        {
            existing.Rating = rating;
            existing.Comment = comment;
            existing.CreatedAt = DateTime.UtcNow;
            _unitOfWork.Reviews.Update(existing);
            await _unitOfWork.SaveChangesAsync();
            return existing;
        }

        var review = new Review
        {
            StudentId = studentId,
            CourseId = courseId,
            Rating = rating,
            Comment = comment,
            CreatedAt = DateTime.UtcNow,
            HelpfulVotesCount = 0
        };

        await _unitOfWork.Reviews.AddAsync(review);
        await _unitOfWork.SaveChangesAsync();
        return review;
    }

    public async Task<int> VoteHelpfulAsync(int reviewId, string userId)
    {
        var existingVote = await _unitOfWork.ReviewHelpfulVotes
            .SingleOrDefaultAsync(v => v.ReviewId == reviewId && v.UserId == userId);

        var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
        if (review == null) throw new InvalidOperationException("Review not found.");

        if (existingVote == null)
        {
            var vote = new ReviewHelpfulVote
            {
                ReviewId = reviewId,
                UserId = userId,
                IsHelpful = true,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.ReviewHelpfulVotes.AddAsync(vote);
            review.HelpfulVotesCount++;
        }
        else
        {
            // Toggle
            _unitOfWork.ReviewHelpfulVotes.Remove(existingVote);
            review.HelpfulVotesCount = Math.Max(0, review.HelpfulVotesCount - 1);
        }

        _unitOfWork.Reviews.Update(review);
        await _unitOfWork.SaveChangesAsync();

        return review.HelpfulVotesCount;
    }

    public async Task<bool> CanStudentReviewCourseAsync(string studentId, int courseId)
    {
        return await _unitOfWork.Enrollments.IsStudentEnrolledAsync(studentId, courseId);
    }
}
