using System.Security.Claims;
using System.Text.Json;
using EduHub.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Web.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private const string CartSessionKey = "EduHub_Cart";
    private const string CouponSessionKey = "EduHub_Coupon";

    private readonly IPaymentService _paymentService;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(IPaymentService paymentService, ILogger<CheckoutController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessCheckout()
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var json = HttpContext.Session.GetString(CartSessionKey);
        var courseIds = string.IsNullOrEmpty(json) ? new List<int>() : JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();

        if (!courseIds.Any())
        {
            TempData["ErrorMessage"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        var couponCode = HttpContext.Session.GetString(CouponSessionKey);

        var scheme = Request.Scheme;
        var host = Request.Host.Value;
        var successUrl = $"{scheme}://{host}/Checkout/Success";
        var cancelUrl = $"{scheme}://{host}/Cart";

        var request = new CheckoutRequest
        {
            StudentId = studentId,
            CourseIds = courseIds,
            CouponCode = couponCode,
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl
        };

        var result = await _paymentService.CreateCheckoutSessionAsync(request);

        // Clear cart session
        HttpContext.Session.Remove(CartSessionKey);
        HttpContext.Session.Remove(CouponSessionKey);

        if (result.IsFree || string.IsNullOrEmpty(result.RedirectUrl))
        {
            return RedirectToAction(nameof(Success), new { sessionId = result.SessionId });
        }

        return Redirect(result.RedirectUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreeEnrollment(int courseId)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        try
        {
            var order = await _paymentService.ProcessDirectFreeEnrollmentAsync(studentId, courseId);
            TempData["SuccessMessage"] = "You have been successfully enrolled in this free course!";
            return RedirectToAction("Watch", "Learning", new { id = courseId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing free enrollment.");
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Details", "Courses", new { id = courseId });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Success(string sessionId)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        try
        {
            var order = await _paymentService.ProcessSuccessfulPaymentAsync(sessionId, studentId);
            var receipt = await _paymentService.GetOrderReceiptAsync(order.Id, studentId);
            return View(receipt ?? order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm payment for session {SessionId}", sessionId);
            TempData["ErrorMessage"] = "Could not verify transaction session.";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var orders = await _paymentService.GetStudentOrdersAsync(studentId);
        return View(orders);
    }
}
