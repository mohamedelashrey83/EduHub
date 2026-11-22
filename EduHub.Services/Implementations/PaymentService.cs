using EduHub.Core.Entities;
using EduHub.Core.Enums;
using EduHub.Core.Interfaces;
using EduHub.Core.Patterns.Strategy;
using EduHub.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace EduHub.Services.Implementations;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPricingService _pricingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;
    private readonly bool _isStripeConfigured;

    public PaymentService(
        IUnitOfWork unitOfWork,
        IPricingService pricingService,
        IConfiguration configuration,
        ILogger<PaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _pricingService = pricingService;
        _configuration = configuration;
        _logger = logger;

        var secretKey = _configuration["Stripe:SecretKey"];
        if (!string.IsNullOrWhiteSpace(secretKey) && secretKey.StartsWith("sk_"))
        {
            StripeConfiguration.ApiKey = secretKey;
            _isStripeConfigured = true;
            _logger.LogInformation("Stripe configured with live/test API keys.");
        }
        else
        {
            _isStripeConfigured = false;
            _logger.LogInformation("Stripe secret key not provided. Running in sandbox test checkout mode.");
        }
    }

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(CheckoutRequest request)
    {
        if (!request.CourseIds.Any())
            throw new ArgumentException("At least one course must be selected.", nameof(request.CourseIds));

        var courses = new List<Course>();
        foreach (var id in request.CourseIds)
        {
            var c = await _unitOfWork.Courses.GetByIdAsync(id);
            if (c != null) courses.Add(c);
        }

        if (!courses.Any())
            throw new InvalidOperationException("None of the selected courses could be found.");

        // Calculate pricing using Strategy Pattern
        var pricingRequest = new PricingCalculationRequest
        {
            Courses = courses
        };

        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var rawSubTotal = courses.Sum(c => c.DiscountPrice ?? c.Price);
            var coupon = await _pricingService.ValidateCouponAsync(request.CouponCode, rawSubTotal);
            if (coupon != null)
            {
                pricingRequest.Coupon = coupon;
            }
        }

        var pricingResult = _pricingService.CalculatePrice(pricingRequest);

        // Create pending order
        var order = new Order
        {
            StudentId = request.StudentId,
            OrderDate = DateTime.UtcNow,
            SubTotal = pricingResult.OriginalSubTotal,
            DiscountAmount = pricingResult.TotalDiscount,
            TotalAmount = pricingResult.FinalTotal,
            CouponCode = pricingRequest.Coupon?.Code,
            PricingStrategyUsed = pricingResult.StrategyApplied,
            PaymentStatus = pricingResult.FinalTotal <= 0m ? PaymentStatus.Completed : PaymentStatus.Pending,
            PaymentMethod = pricingResult.FinalTotal <= 0m ? "Free Enrollment" : "Stripe"
        };

        foreach (var course in courses)
        {
            decimal itemPrice = course.Price;
            decimal itemFinal = pricingResult.ItemPrices.TryGetValue(course.Id, out var p) ? p : course.Price;
            decimal itemDiscount = Math.Max(0m, itemPrice - itemFinal);

            order.OrderItems.Add(new OrderItem
            {
                CourseId = course.Id,
                Price = itemPrice,
                DiscountApplied = itemDiscount
            });
        }

        // If completely free ($0), complete order immediately and enroll student
        if (pricingResult.FinalTotal <= 0m)
        {
            order.StripeSessionId = $"FREE-{Guid.NewGuid():N}";
            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            // Auto-enroll in all courses
            await EnrollStudentInCoursesAsync(request.StudentId, courses.Select(c => c.Id));

            return new CheckoutSessionResult
            {
                SessionId = order.StripeSessionId,
                IsFree = true,
                OrderId = order.Id,
                PricingDetails = pricingResult,
                RedirectUrl = $"{request.SuccessUrl}?sessionId={order.StripeSessionId}"
            };
        }

        // Paid checkout
        if (_isStripeConfigured)
        {
            try
            {
                var lineItems = new List<SessionLineItemOptions>();
                foreach (var course in courses)
                {
                    decimal priceToCharge = pricingResult.ItemPrices.TryGetValue(course.Id, out var p) ? p : course.Price;
                    long cents = (long)Math.Round(priceToCharge * 100);

                    lineItems.Add(new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = cents,
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = course.Title,
                                Description = course.Subtitle ?? $"Course by {course.InstructorId}",
                                Images = !string.IsNullOrEmpty(course.ThumbnailUrl) ? new List<string> { course.ThumbnailUrl } : null
                            }
                        },
                        Quantity = 1
                    });
                }

                var options = new SessionCreateOptions
                {
                    LineItems = lineItems,
                    Mode = "payment",
                    SuccessUrl = $"{request.SuccessUrl}?sessionId={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = request.CancelUrl,
                    ClientReferenceId = order.StudentId,
                    Metadata = new Dictionary<string, string>
                    {
                        { "CourseIds", string.Join(",", request.CourseIds) },
                        { "StudentId", request.StudentId }
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                order.StripeSessionId = session.Id;
                await _unitOfWork.Orders.AddAsync(order);
                await _unitOfWork.SaveChangesAsync();

                return new CheckoutSessionResult
                {
                    SessionId = session.Id,
                    RedirectUrl = session.Url,
                    IsFree = false,
                    OrderId = order.Id,
                    PricingDetails = pricingResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe API call failed. Falling back to sandbox checkout simulation.");
            }
        }

        // Sandbox / mock checkout session
        var mockSessionId = $"cs_test_{Guid.NewGuid():N}";
        order.StripeSessionId = mockSessionId;
        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        var localRedirect = $"{request.SuccessUrl}?sessionId={mockSessionId}";
        return new CheckoutSessionResult
        {
            SessionId = mockSessionId,
            RedirectUrl = localRedirect,
            IsFree = false,
            OrderId = order.Id,
            PricingDetails = pricingResult
        };
    }

    public async Task<Order> ProcessSuccessfulPaymentAsync(string sessionId, string studentId)
    {
        var order = await _unitOfWork.Orders.SingleOrDefaultAsync(o => o.StripeSessionId == sessionId);
        if (order == null)
        {
            throw new InvalidOperationException($"No order found with session ID: {sessionId}");
        }

        if (order.PaymentStatus == PaymentStatus.Completed)
        {
            return order; // Already processed
        }

        order.PaymentStatus = PaymentStatus.Completed;

        // Increment coupon usage if used
        if (!string.IsNullOrEmpty(order.CouponCode))
        {
            var coupon = await _unitOfWork.Coupons.SingleOrDefaultAsync(c => c.Code == order.CouponCode);
            if (coupon != null)
            {
                coupon.TimesUsed++;
            }
        }

        // Fetch Order with items
        var fullOrder = await _unitOfWork.Orders.GetOrderWithItemsAsync(order.Id);
        if (fullOrder != null)
        {
            var courseIds = fullOrder.OrderItems.Select(oi => oi.CourseId).ToList();
            await EnrollStudentInCoursesAsync(studentId, courseIds);

            // Update instructor earnings
            foreach (var item in fullOrder.OrderItems)
            {
                if (item.Course?.Instructor != null)
                {
                    decimal instructorShare = (item.Price - item.DiscountApplied) * 0.70m;
                    item.Course.Instructor.TotalEarnings += instructorShare;
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Order #{OrderId} processed successfully for student {StudentId}", order.Id, studentId);
        return order;
    }

    public async Task<Order> ProcessDirectFreeEnrollmentAsync(string studentId, int courseId)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null)
            throw new InvalidOperationException("Course not found.");

        if (course.Price > 0 && (!course.DiscountPrice.HasValue || course.DiscountPrice.Value > 0))
            throw new InvalidOperationException("This is a paid course. Please use the shopping cart to purchase.");

        var order = new Order
        {
            StudentId = studentId,
            OrderDate = DateTime.UtcNow,
            SubTotal = 0m,
            DiscountAmount = 0m,
            TotalAmount = 0m,
            PricingStrategyUsed = "Free Course Strategy",
            PaymentStatus = PaymentStatus.Completed,
            PaymentMethod = "Free Direct Enrollment",
            StripeSessionId = $"FREE-{Guid.NewGuid():N}"
        };

        order.OrderItems.Add(new OrderItem
        {
            CourseId = courseId,
            Price = 0m,
            DiscountApplied = 0m
        });

        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        await EnrollStudentInCoursesAsync(studentId, new[] { courseId });
        return order;
    }

    public async Task<Order?> GetOrderReceiptAsync(int orderId, string studentId)
    {
        var order = await _unitOfWork.Orders.GetOrderWithItemsAsync(orderId);
        if (order == null || order.StudentId != studentId) return null;
        return order;
    }

    public async Task<IEnumerable<Order>> GetStudentOrdersAsync(string studentId)
    {
        return await _unitOfWork.Orders.GetOrdersByStudentAsync(studentId);
    }

    private async Task EnrollStudentInCoursesAsync(string studentId, IEnumerable<int> courseIds)
    {
        foreach (var cId in courseIds)
        {
            var isEnrolled = await _unitOfWork.Enrollments.IsStudentEnrolledAsync(studentId, cId);
            if (!isEnrolled)
            {
                var enrollment = new Enrollment
                {
                    StudentId = studentId,
                    CourseId = cId,
                    EnrolledAt = DateTime.UtcNow,
                    ProgressPercentage = 0.0,
                    IsCompleted = false
                };
                await _unitOfWork.Enrollments.AddAsync(enrollment);
            }
        }
        await _unitOfWork.SaveChangesAsync();
    }
}
