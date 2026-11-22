using System.Security.Claims;
using EduHub.Core.Interfaces;
using EduHub.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Web.Controllers;

[Authorize]
public class LearningController : Controller
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly ICourseService _courseService;
    private readonly IQuizService _quizService;
    private readonly IStorageService _storageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LearningController> _logger;

    public LearningController(
        IEnrollmentService enrollmentService,
        ICourseService courseService,
        IQuizService quizService,
        IStorageService storageService,
        IUnitOfWork unitOfWork,
        ILogger<LearningController> logger)
    {
        _enrollmentService = enrollmentService;
        _courseService = courseService;
        _quizService = quizService;
        _storageService = storageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Watch(int id, int? lessonId = null)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Check if student enrolled
        var isEnrolled = await _unitOfWork.Enrollments.IsStudentEnrolledAsync(studentId, id);
        var course = await _courseService.GetCourseDetailsAsync(id);

        if (course == null) return NotFound("Course not found.");

        // If instructor or admin, allow previewing full course
        if (!isEnrolled && (course.InstructorId == studentId || User.IsInRole("Admin")))
        {
            // Auto-enroll instructor/admin for viewing
            var enrollment = new Core.Entities.Enrollment
            {
                StudentId = studentId,
                CourseId = id,
                EnrolledAt = DateTime.UtcNow,
                ProgressPercentage = 0.0
            };
            await _unitOfWork.Enrollments.AddAsync(enrollment);
            await _unitOfWork.SaveChangesAsync();
            isEnrolled = true;
        }

        if (!isEnrolled)
        {
            TempData["ErrorMessage"] = "You must be enrolled in this course to access the classroom.";
            return RedirectToAction("Details", "Courses", new { id });
        }

        var dashboard = await _enrollmentService.GetLearningDashboardAsync(studentId, id, lessonId);
        if (dashboard == null) return NotFound("Learning content not found.");

        // Generate signed streaming URL for the active lesson video
        if (!string.IsNullOrEmpty(dashboard.CurrentLesson.VideoUrl))
        {
            ViewBag.StreamingVideoUrl = _storageService.GenerateSecureStreamingUrl(dashboard.CurrentLesson.VideoUrl);
        }

        return View(dashboard);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleComplete(int lessonId, bool isCompleted)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        try
        {
            var newProgress = await _enrollmentService.MarkLessonCompleteAsync(studentId, lessonId, isCompleted);
            return Json(new { success = true, progress = newProgress, isCompleted });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle lesson completion.");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePosition(int lessonId, int seconds)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _enrollmentService.UpdateWatchPositionAsync(studentId, lessonId, seconds);
        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNote(int lessonId, int timestampSeconds, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Json(new { success = false, message = "Note content cannot be empty." });

        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var note = await _enrollmentService.AddNoteAsync(studentId, lessonId, timestampSeconds, content);

        return Json(new
        {
            success = true,
            id = note.Id,
            timestampSeconds = note.TimestampSeconds,
            formattedTime = TimeSpan.FromSeconds(note.TimestampSeconds).ToString(@"mm\:ss"),
            content = note.Content,
            createdAt = note.CreatedAt.ToString("MMM dd, yyyy")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteNote(int noteId)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _enrollmentService.DeleteNoteAsync(noteId, studentId);
        return Json(new { success = result });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBookmark(int lessonId, string? note = null)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isBookmarked = await _enrollmentService.ToggleBookmarkAsync(studentId, lessonId, note);
        return Json(new { success = true, isBookmarked });
    }

    [HttpGet]
    public async Task<IActionResult> Quiz(int id)
    {
        var quiz = await _quizService.GetQuizByIdAsync(id);
        if (quiz == null) return NotFound("Quiz not found.");

        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var attempts = await _quizService.GetStudentQuizAttemptsAsync(studentId, id);

        ViewBag.PastAttempts = attempts;
        return View(quiz);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitQuiz(int id, IFormCollection form)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var answersDict = new Dictionary<int, string>();
        foreach (var key in form.Keys)
        {
            if (key.StartsWith("question_") && int.TryParse(key.Replace("question_", ""), out int questionId))
            {
                answersDict[questionId] = form[key].ToString();
            }
        }

        var result = await _quizService.SubmitQuizAttemptAsync(studentId, id, answersDict);
        return View("QuizResult", result);
    }

    [HttpGet]
    public async Task<IActionResult> MyCourses()
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var enrollments = (await _enrollmentService.GetStudentCoursesAsync(studentId)).ToList();
        return View(enrollments);
    }
}
