using System.Security.Claims;
using EduHub.Core.Entities;
using EduHub.Core.Enums;
using EduHub.Core.Interfaces;
using EduHub.Services.Interfaces;
using EduHub.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Web.Controllers;

[Authorize(Roles = $"{UserRoles.Instructor},{UserRoles.Admin}")]
public class InstructorController : Controller
{
    private readonly ICourseService _courseService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IStorageService _storageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<InstructorController> _logger;

    public InstructorController(
        ICourseService courseService,
        IAnalyticsService analyticsService,
        IStorageService storageService,
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        ILogger<InstructorController> logger)
    {
        _courseService = courseService;
        _analyticsService = analyticsService;
        _storageService = storageService;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var instructor = await _userManager.FindByIdAsync(instructorId);
        if (instructor == null) return NotFound();

        var analytics = await _analyticsService.GetInstructorAnalyticsAsync(instructorId);
        var courses = await _courseService.GetInstructorCoursesAsync(instructorId);

        var viewModel = new InstructorDashboardViewModel
        {
            Instructor = instructor,
            Analytics = analytics,
            Courses = courses
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Courses()
    {
        var instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var courses = await _courseService.GetInstructorCoursesAsync(instructorId);
        return View(courses);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var categories = await _unitOfWork.Categories.GetAllAsync();
        var model = new CreateCourseViewModel
        {
            AvailableCategories = categories
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCourseViewModel model)
    {
        var instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!ModelState.IsValid)
        {
            model.AvailableCategories = await _unitOfWork.Categories.GetAllAsync();
            return View(model);
        }

        // Handle file uploads (Azure Blob Storage or local fallback)
        string? thumbUrl = model.ThumbnailUrl;
        if (model.ThumbnailFile != null && model.ThumbnailFile.Length > 0)
        {
            thumbUrl = await _storageService.UploadFileAsync(model.ThumbnailFile, "thumbnails");
        }

        string? promoUrl = model.PromoVideoUrl;
        if (model.PromoVideoFile != null && model.PromoVideoFile.Length > 0)
        {
            promoUrl = await _storageService.UploadFileAsync(model.PromoVideoFile, "promo_videos");
        }

        var course = new Course
        {
            Title = model.Title,
            Subtitle = model.Subtitle,
            Description = model.Description,
            Requirements = model.Requirements,
            WhatYouWillLearn = model.WhatYouWillLearn,
            Price = model.Price,
            DiscountPrice = model.DiscountPrice,
            Level = model.Level,
            CategoryId = model.CategoryId,
            SubcategoryId = model.SubcategoryId,
            ThumbnailUrl = thumbUrl ?? "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?w=600",
            PromoVideoUrl = promoUrl,
            Language = model.Language
        };

        var created = await _courseService.CreateCourseAsync(course, instructorId);
        TempData["SuccessMessage"] = "Course draft created! Now design your curriculum below.";
        return RedirectToAction(nameof(Curriculum), new { id = created.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var course = await _courseService.GetCourseDetailsAsync(id);
        if (course == null || (course.InstructorId != instructorId && !User.IsInRole("Admin")))
            return NotFound("Course not found or unauthorized.");

        var categories = await _unitOfWork.Categories.GetAllAsync();
        var model = new EditCourseViewModel
        {
            Id = course.Id,
            Title = course.Title,
            Subtitle = course.Subtitle,
            Description = course.Description,
            Requirements = course.Requirements,
            WhatYouWillLearn = course.WhatYouWillLearn,
            Price = course.Price,
            DiscountPrice = course.DiscountPrice,
            Level = course.Level,
            Status = course.Status,
            CategoryId = course.CategoryId,
            SubcategoryId = course.SubcategoryId,
            ThumbnailUrl = course.ThumbnailUrl,
            PromoVideoUrl = course.PromoVideoUrl,
            Language = course.Language,
            AvailableCategories = categories
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditCourseViewModel model)
    {
        var instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!ModelState.IsValid)
        {
            model.AvailableCategories = await _unitOfWork.Categories.GetAllAsync();
            return View(model);
        }

        string? thumbUrl = model.ThumbnailUrl;
        if (model.ThumbnailFile != null && model.ThumbnailFile.Length > 0)
        {
            thumbUrl = await _storageService.UploadFileAsync(model.ThumbnailFile, "thumbnails");
        }

        string? promoUrl = model.PromoVideoUrl;
        if (model.PromoVideoFile != null && model.PromoVideoFile.Length > 0)
        {
            promoUrl = await _storageService.UploadFileAsync(model.PromoVideoFile, "promo_videos");
        }

        var course = new Course
        {
            Id = model.Id,
            Title = model.Title,
            Subtitle = model.Subtitle,
            Description = model.Description,
            Requirements = model.Requirements,
            WhatYouWillLearn = model.WhatYouWillLearn,
            Price = model.Price,
            DiscountPrice = model.DiscountPrice,
            Level = model.Level,
            CategoryId = model.CategoryId,
            SubcategoryId = model.SubcategoryId,
            ThumbnailUrl = thumbUrl,
            PromoVideoUrl = promoUrl,
            Language = model.Language
        };

        await _courseService.UpdateCourseAsync(course, instructorId);
        TempData["SuccessMessage"] = "Course details updated successfully!";
        return RedirectToAction(nameof(Courses));
    }

    [HttpGet]
    public async Task<IActionResult> Curriculum(int id)
    {
        var instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var course = await _courseService.GetCourseForCurriculumEditorAsync(id, instructorId);
        if (course == null && User.IsInRole("Admin"))
        {
            course = await _courseService.GetCourseDetailsAsync(id);
        }

        if (course == null) return NotFound("Course not found or unauthorized.");

        var model = new CurriculumBuilderViewModel
        {
            Course = course,
            Sections = course.Sections.OrderBy(s => s.OrderIndex).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSection([FromBody] AddSectionViewModel model)
    {
        var sec = await _courseService.AddSectionAsync(model.CourseId, model.Title, model.OrderIndex);
        return Json(new { success = true, sectionId = sec.Id, title = sec.Title, orderIndex = sec.OrderIndex });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSection(int sectionId)
    {
        var result = await _courseService.DeleteSectionAsync(sectionId);
        return Json(new { success = result });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(1024L * 1024L * 1024L)]
    [RequestFormLimits(MultipartBodyLengthLimit = 1024L * 1024L * 1024L)]
    public async Task<IActionResult> AddLesson([FromForm] AddLessonViewModel model)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                return BadRequest(new { success = false, message = "Lecture title is required." });
            }

            string? videoUrl = model.VideoUrl;
            if (model.VideoFile != null && model.VideoFile.Length > 0)
            {
                videoUrl = await _storageService.UploadFileAsync(model.VideoFile, "lessons_videos");
            }

            var lesson = new Lesson
            {
                SectionId = model.SectionId,
                Title = model.Title.Trim(),
                Description = model.Description,
                VideoUrl = videoUrl,
                DurationSeconds = model.DurationMinutes > 0 ? model.DurationMinutes * 60 : 600,
                IsFreePreview = model.IsFreePreview,
                ContentNotes = model.ContentNotes,
                OrderIndex = 99
            };

            var created = await _courseService.AddLessonAsync(lesson);
            return Json(new
            {
                success = true,
                lessonId = created.Id,
                title = created.Title,
                durationMinutes = created.DurationMinutes,
                isFreePreview = created.IsFreePreview,
                videoUrl = created.VideoUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload/save lecture for SectionId {SectionId}", model.SectionId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLesson(int lessonId)
    {
        var result = await _courseService.DeleteLessonAsync(lessonId);
        return Json(new { success = result });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuiz([FromBody] AddQuizViewModel model)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                return BadRequest(new { success = false, message = "Quiz title is required." });
            }

            var quiz = new Quiz
            {
                CourseId = model.CourseId,
                SectionId = model.SectionId,
                Title = model.Title.Trim(),
                Description = model.Description,
                PassingScorePercentage = model.PassingScorePercentage > 0 ? model.PassingScorePercentage : 70
            };

            int qIndex = 1;
            foreach (var qModel in model.Questions)
            {
                if (string.IsNullOrWhiteSpace(qModel.QuestionText)) continue;

                var question = new QuizQuestion
                {
                    QuestionText = qModel.QuestionText.Trim(),
                    QuestionType = qModel.QuestionType,
                    Explanation = qModel.Explanation,
                    Points = qModel.Points > 0 ? qModel.Points : 1,
                    OrderIndex = qIndex++
                };

                foreach (var aModel in qModel.Answers)
                {
                    if (string.IsNullOrWhiteSpace(aModel.AnswerText)) continue;

                    question.Answers.Add(new QuizAnswer
                    {
                        AnswerText = aModel.AnswerText.Trim(),
                        IsCorrect = aModel.IsCorrect
                    });
                }

                quiz.Questions.Add(question);
            }

            await _unitOfWork.Quizzes.AddAsync(quiz);
            await _unitOfWork.SaveChangesAsync();

            return Json(new
            {
                success = true,
                quizId = quiz.Id,
                title = quiz.Title,
                passingScore = quiz.PassingScorePercentage,
                questionsCount = quiz.Questions.Count,
                sectionId = model.SectionId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create quiz for SectionId {SectionId}", model.SectionId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuiz(int quizId)
    {
        try
        {
            var quiz = await _unitOfWork.Quizzes.GetByIdAsync(quizId);
            if (quiz == null) return NotFound(new { success = false, message = "Quiz not found." });

            _unitOfWork.Quizzes.Remove(quiz);
            await _unitOfWork.SaveChangesAsync();

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete quiz #{QuizId}", quizId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderCurriculum(int courseId, [FromBody] List<SectionReorderDto> reorderList)
    {
        var result = await _courseService.ReorderCurriculumAsync(courseId, reorderList);
        return Json(new { success = result });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitApproval(int id)
    {
        var instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _courseService.SubmitCourseForApprovalAsync(id, instructorId);
        if (result)
        {
            TempData["SuccessMessage"] = "Course submitted for Admin review and quality control.";
        }
        return RedirectToAction(nameof(Courses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id)
    {
        var instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _courseService.PublishCourseAsync(id, instructorId);
        if (result)
        {
            TempData["SuccessMessage"] = "Course is now published and live on the catalog!";
        }
        return RedirectToAction(nameof(Courses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _courseService.DeleteCourseAsync(id, instructorId);
        if (result)
        {
            TempData["SuccessMessage"] = "Course deleted successfully.";
        }
        return RedirectToAction(nameof(Courses));
    }
}
