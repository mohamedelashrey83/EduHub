using System.ComponentModel.DataAnnotations.Schema;
using EduHub.Core.Enums;

namespace EduHub.Core.Entities;

public class Order
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public virtual ApplicationUser? Student { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public string? CouponCode { get; set; }
    public string? PricingStrategyUsed { get; set; }

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string PaymentMethod { get; set; } = "Stripe";
    public string? StripePaymentIntentId { get; set; }
    public string? StripeSessionId { get; set; }

    // Navigation properties
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public virtual Order? Order { get; set; }

    public int CourseId { get; set; }
    public virtual Course? Course { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountApplied { get; set; }
}

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountValue { get; set; }

    public DateTime? ExpiryDate { get; set; }
    public int? UsageLimit { get; set; }
    public int TimesUsed { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinimumSpend { get; set; }
}
