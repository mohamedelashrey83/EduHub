using System.Security.Claims;
using System.Text.Json;
using EduHub.Core.Entities;
using EduHub.Core.Interfaces;
using EduHub.Core.Patterns.Strategy;
using EduHub.Services.Interfaces;
using EduHub.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Web.Controllers;

public class CartController : Controller
{
    private const string CartSessionKey = "EduHub_Cart";
    private const string CouponSessionKey = "EduHub_Coupon";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPricingService _pricingService;

    public CartController(IUnitOfWork unitOfWork, IPricingService pricingService)
    {
        _unitOfWork = unitOfWork;
        _pricingService = pricingService;
    }

    private List<int> GetCartItemIds()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        return string.IsNullOrEmpty(json) ? new List<int>() : JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
    }

    private void SaveCartItemIds(List<int> ids)
    {
        HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(ids.Distinct().ToList()));
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var ids = GetCartItemIds();
        var couponCode = HttpContext.Session.GetString(CouponSessionKey);

        if (User.Identity?.IsAuthenticated == true)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(studentId))
            {
                var enrolledCourseIds = (await _unitOfWork.Enrollments.FindAsync(e => e.StudentId == studentId))
                    .Select(e => e.CourseId)
                    .ToHashSet();

                var duplicateIds = ids.Where(id => enrolledCourseIds.Contains(id)).ToList();
                if (duplicateIds.Any())
                {
                    ids = ids.Except(duplicateIds).ToList();
                    SaveCartItemIds(ids);
                    TempData["InfoMessage"] = "Courses you are already subscribed to were removed from your cart.";
                }
            }
        }

        var courses = new List<Course>();
        foreach (var id in ids)
        {
            var c = await _unitOfWork.Courses.GetCourseWithFullCurriculumAsync(id);
            if (c != null) courses.Add(c);
        }

        var pricingRequest = new PricingCalculationRequest
        {
            Courses = courses
        };

        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var rawSub = courses.Sum(c => c.DiscountPrice ?? c.Price);
            var coupon = await _pricingService.ValidateCouponAsync(couponCode, rawSub);
            if (coupon != null)
            {
                pricingRequest.Coupon = coupon;
            }
        }

        var pricingResult = _pricingService.CalculatePrice(pricingRequest);

        var viewModel = new CartViewModel
        {
            Items = courses.Select(c => new CartItemDto
            {
                CourseId = c.Id,
                Title = c.Title,
                InstructorName = c.Instructor?.FullName ?? "Instructor",
                ThumbnailUrl = c.ThumbnailUrl,
                OriginalPrice = c.Price,
                EffectivePrice = pricingResult.ItemPrices.TryGetValue(c.Id, out var p) ? p : (c.DiscountPrice ?? c.Price),
                Level = c.Level
            }).ToList(),
            SubTotal = pricingResult.OriginalSubTotal,
            TotalDiscount = pricingResult.TotalDiscount,
            FinalTotal = pricingResult.FinalTotal,
            CouponCode = couponCode,
            PricingStrategyApplied = pricingResult.StrategyApplied,
            PricingDescription = pricingResult.Description
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int courseId)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null) return NotFound("Course not found.");

        if (User.Identity?.IsAuthenticated == true)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(studentId))
            {
                var existingEnrollment = await _unitOfWork.Enrollments.GetEnrollmentByStudentAndCourseAsync(studentId, courseId);
                if (existingEnrollment != null)
                {
                    TempData["InfoMessage"] = $"You are already subscribed to '{course.Title}'.";
                    return RedirectToAction("Watch", "Learning", new { id = courseId });
                }
            }
        }

        var ids = GetCartItemIds();
        if (!ids.Contains(courseId))
        {
            ids.Add(courseId);
            SaveCartItemIds(ids);
            TempData["SuccessMessage"] = $"'{course.Title}' was added to your cart!";
        }
        else
        {
            TempData["InfoMessage"] = $"'{course.Title}' is already in your cart.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int courseId)
    {
        var ids = GetCartItemIds();
        if (ids.Remove(courseId))
        {
            SaveCartItemIds(ids);
            TempData["SuccessMessage"] = "Course removed from cart.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyCoupon(string couponCode)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            HttpContext.Session.Remove(CouponSessionKey);
            TempData["InfoMessage"] = "Coupon cleared.";
            return RedirectToAction(nameof(Index));
        }

        var ids = GetCartItemIds();
        var courses = new List<Course>();
        foreach (var id in ids)
        {
            var c = await _unitOfWork.Courses.GetByIdAsync(id);
            if (c != null) courses.Add(c);
        }

        decimal rawSubTotal = courses.Sum(c => c.DiscountPrice ?? c.Price);
        var coupon = await _pricingService.ValidateCouponAsync(couponCode, rawSubTotal);

        if (coupon != null)
        {
            HttpContext.Session.SetString(CouponSessionKey, coupon.Code);
            TempData["SuccessMessage"] = $"Coupon '{coupon.Code}' applied successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Invalid or expired coupon code.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        HttpContext.Session.Remove(CartSessionKey);
        HttpContext.Session.Remove(CouponSessionKey);
        return RedirectToAction(nameof(Index));
    }
}
