using EduHub.Core.Entities;
using EduHub.Core.Enums;
using EduHub.Core.Interfaces;
using EduHub.Services.Interfaces;
using EduHub.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Web.Controllers;

[Authorize(Roles = UserRoles.Admin)]
public class AdminController : Controller
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ICourseService _courseService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IAnalyticsService analyticsService,
        ICourseService courseService,
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger)
    {
        _analyticsService = analyticsService;
        _courseService = courseService;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var analytics = await _analyticsService.GetAdminAnalyticsAsync();
        var pendingCourses = await _unitOfWork.Courses.GetPendingApprovalCoursesAsync();
        var recentUsers = (await _unitOfWork.Users.GetAllAsync()).OrderByDescending(u => u.CreatedAt).Take(5);
        var categories = await _unitOfWork.Categories.GetAllAsync();

        var viewModel = new AdminDashboardViewModel
        {
            Analytics = analytics,
            PendingCourses = pendingCourses,
            RecentUsers = recentUsers,
            Categories = categories
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Courses()
    {
        var allCourses = await _unitOfWork.Courses.GetAllAsync();
        return View(allCourses.OrderBy(c => c.Status == CourseStatus.PendingApproval ? 0 : 1).ThenByDescending(c => c.CreatedAt));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveCourse(int id)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(id);
        if (course == null) return NotFound("Course not found.");

        course.Status = CourseStatus.Published;
        course.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Courses.Update(course);
        await _unitOfWork.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Course '{course.Title}' has been approved and published to the catalog!";
        return RedirectToAction(nameof(Courses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectCourse(int id, string? reason)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(id);
        if (course == null) return NotFound("Course not found.");

        course.Status = CourseStatus.Rejected;
        course.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Courses.Update(course);
        await _unitOfWork.SaveChangesAsync();

        TempData["InfoMessage"] = $"Course '{course.Title}' was rejected. Feedback: {reason ?? "Curriculum requires revision."}";
        return RedirectToAction(nameof(Courses));
    }

    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var users = (await _unitOfWork.Users.GetAllAsync()).ToList();
        var userListWithRoles = new List<(ApplicationUser User, IList<string> Roles)>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            userListWithRoles.Add((u, roles));
        }

        return View(userListWithRoles);
    }

    [HttpGet]
    public async Task<IActionResult> Categories()
    {
        var categories = await _unitOfWork.Categories.GetAllAsync();
        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(string name, string? description, string? iconClass, int? parentCategoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Category name is required.";
            return RedirectToAction(nameof(Categories));
        }

        var slug = name.Trim().ToLower().Replace(" ", "-").Replace("&", "and");
        var category = new Category
        {
            Name = name,
            Slug = slug,
            Description = description,
            IconClass = string.IsNullOrWhiteSpace(iconClass) ? "bi-mortarboard" : iconClass,
            ParentCategoryId = parentCategoryId > 0 ? parentCategoryId : null
        };

        await _unitOfWork.Categories.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Category '{name}' created successfully!";
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id);
        if (category != null)
        {
            _unitOfWork.Categories.Remove(category);
            await _unitOfWork.SaveChangesAsync();
            TempData["SuccessMessage"] = "Category deleted.";
        }
        return RedirectToAction(nameof(Categories));
    }

    [HttpGet]
    public async Task<IActionResult> Reports()
    {
        var orders = (await _unitOfWork.Orders.GetAllAsync()).OrderByDescending(o => o.OrderDate).ToList();
        var totalRev = await _unitOfWork.Orders.GetTotalPlatformRevenueAsync();
        var coupons = await _unitOfWork.Coupons.GetAllAsync();

        ViewBag.TotalRevenue = totalRev;
        ViewBag.Coupons = coupons;
        return View(orders);
    }
}
