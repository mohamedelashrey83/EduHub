using EduHub.Core.Entities;
using EduHub.Core.Patterns.Strategy;

namespace EduHub.Services.Interfaces;

public class CheckoutRequest
{
    public string StudentId { get; set; } = string.Empty;
    public List<int> CourseIds { get; set; } = new();
    public string? CouponCode { get; set; }
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}

public class CheckoutSessionResult
{
    public string SessionId { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
    public bool IsFree { get; set; }
    public int OrderId { get; set; }
    public PriceCalculationResult PricingDetails { get; set; } = new();
}

public interface IPaymentService
{
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(CheckoutRequest request);
    Task<Order> ProcessSuccessfulPaymentAsync(string sessionId, string studentId);
    Task<Order> ProcessDirectFreeEnrollmentAsync(string studentId, int courseId);
    Task<Order?> GetOrderReceiptAsync(int orderId, string studentId);
    Task<IEnumerable<Order>> GetStudentOrdersAsync(string studentId);
}
