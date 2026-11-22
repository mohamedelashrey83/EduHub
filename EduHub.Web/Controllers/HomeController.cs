using System.Diagnostics;
using System.Security.Claims;
using EduHub.Core.Interfaces;
using EduHub.Services.Interfaces;
using EduHub.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Web.Controllers;

public class HomeController : Controller
{
    private readonly ICourseService _courseService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ICourseService courseService,
        IUnitOfWork unitOfWork,
        ILogger<HomeController> logger)
    {
        _courseService = courseService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var featuredCourses = await _courseService.GetFeaturedCoursesAsync(8);
        var categories = await _unitOfWork.Categories.FindAsync(c => c.ParentCategoryId == null);
        var totalCourses = await _unitOfWork.Courses.CountAsync(c => c.Status == Core.Enums.CourseStatus.Published);
        var totalEnrollments = await _unitOfWork.Enrollments.CountAsync();
        var instructors = (await _unitOfWork.Users.FindAsync(u => u.AuthoredCourses.Any())).Take(4);

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

        ViewBag.Categories = categories;
        ViewBag.TotalCourses = totalCourses;
        ViewBag.TotalEnrollments = totalEnrollments;
        ViewBag.TopInstructors = instructors;
        ViewBag.EnrolledCourseIds = enrolledCourseIds;

        return View(featuredCourses);
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
