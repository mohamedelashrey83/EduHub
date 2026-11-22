using System.Security.Claims;
using EduHub.Core.Enums;
using EduHub.Core.Interfaces;
using EduHub.Services.Interfaces;
using EduHub.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Web.Controllers;

public class CoursesController : Controller
{
    private readonly ICourseService _courseService;
    private readonly IReviewService _reviewService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CoursesController> _logger;

    public CoursesController(
        ICourseService courseService,
        IReviewService reviewService,
        IUnitOfWork unitOfWork,
        ILogger<CoursesController> logger)
    {
        _courseService = courseService;
        _reviewService = reviewService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int? categoryId = null,
        string? search = null,
        CourseLevel? level = null,
        bool? isFree = null,
        string? sortBy = null)
    {
        var courses = (await _courseService.GetCatalogCoursesAsync(categoryId, search, level, isFree, sortBy)).ToList();
        var categories = await _unitOfWork.Categories.GetAllAsync();

        var enrolledCourseIds = new HashSet<int>();
        if (User.Identity?.IsAuthenticated == true)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var enrollments = await _unitOfWork.Enrollments.FindAsync(e => e.StudentId == currentUserId);
                enrolledCourseIds = enrollments.Select(e => e.CourseId).ToHashSet();
            }
        }

        var viewModel = new CourseCatalogViewModel
        {
            Courses = courses,
            Categories = categories,
            SearchQuery = search,
            SelectedCategoryId = categoryId,
            SelectedLevel = level,
            IsFree = isFree,
            SortBy = sortBy,
            TotalCount = courses.Count,
            EnrolledCourseIds = enrolledCourseIds
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var course = await _courseService.GetCourseDetailsAsync(id);
        if (course == null) return NotFound("Course not found.");

        var reviewsSummary = await _reviewService.GetCourseReviewsSummaryAsync(id);

        string? currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool isEnrolled = false;
        bool isInstructor = false;
        bool canReview = false;
        Core.Entities.Review? userReview = null;
        Core.Entities.Enrollment? enrollment = null;

        if (!string.IsNullOrEmpty(currentUserId))
        {
            enrollment = await _unitOfWork.Enrollments.GetEnrollmentByStudentAndCourseAsync(currentUserId, id);
            isEnrolled = enrollment != null;
            isInstructor = course.InstructorId == currentUserId;
            canReview = isEnrolled;
            if (canReview)
            {
                userReview = await _unitOfWork.Reviews.GetUserReviewForCourseAsync(currentUserId, id);
            }
        }

        int totalStudents = await _unitOfWork.Enrollments.CountAsync(e => e.CourseId == id);

        var viewModel = new CourseDetailsViewModel
        {
            Course = course,
            ReviewsSummary = reviewsSummary,
            IsEnrolled = isEnrolled,
            IsInstructorOfCourse = isInstructor,
            CanReview = canReview,
            UserReview = userReview,
            UserEnrollment = enrollment,
            TotalStudentsCount = totalStudents
        };

        return View(viewModel);
    }
}
