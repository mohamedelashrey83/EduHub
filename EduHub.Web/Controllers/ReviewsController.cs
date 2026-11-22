using System.Security.Claims;
using EduHub.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Web.Controllers;

[Authorize]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int courseId, int rating, string comment)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var canReview = await _reviewService.CanStudentReviewCourseAsync(studentId, courseId);
        if (!canReview)
        {
            TempData["ErrorMessage"] = "You must be enrolled in this course to leave a review.";
            return RedirectToAction("Details", "Courses", new { id = courseId });
        }

        if (rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(comment))
        {
            TempData["ErrorMessage"] = "Please select a rating between 1 and 5 stars and write a short review.";
            return RedirectToAction("Details", "Courses", new { id = courseId });
        }

        await _reviewService.AddOrUpdateReviewAsync(studentId, courseId, rating, comment);
        TempData["SuccessMessage"] = "Thank you! Your course review has been published.";
        return RedirectToAction("Details", "Courses", new { id = courseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VoteHelpful(int reviewId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var newCount = await _reviewService.VoteHelpfulAsync(reviewId, userId);
        return Json(new { success = true, helpfulCount = newCount });
    }
}
