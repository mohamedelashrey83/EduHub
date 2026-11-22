using EduHub.Core.Entities;
using EduHub.Core.Enums;

namespace EduHub.Core.Patterns.Strategy;

public class PricingCalculationRequest
{
    public List<Course> Courses { get; set; } = new();
    public Coupon? Coupon { get; set; }
    public decimal SeasonalDiscountPercentage { get; set; } = 0m;
    public string? SeasonalCampaignName { get; set; }
}

public class PriceCalculationResult
{
    public decimal OriginalSubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal FinalTotal { get; set; }
    public string StrategyApplied { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<int, decimal> ItemPrices { get; set; } = new();
}

public interface IPricingStrategy
{
    string StrategyName { get; }
    bool CanApply(PricingCalculationRequest request);
    PriceCalculationResult CalculatePrice(PricingCalculationRequest request);
}

/// <summary>
/// Free course pricing (all courses free or total equals 0)
/// </summary>
public class FreeCourseStrategy : IPricingStrategy
{
    public string StrategyName => "Free Course Strategy";

    public bool CanApply(PricingCalculationRequest request)
    {
        return request.Courses.Any() && request.Courses.All(c => c.Price == 0 || (c.DiscountPrice.HasValue && c.DiscountPrice.Value == 0));
    }

    public PriceCalculationResult CalculatePrice(PricingCalculationRequest request)
    {
        var result = new PriceCalculationResult
        {
            OriginalSubTotal = 0m,
            TotalDiscount = 0m,
            FinalTotal = 0m,
            StrategyApplied = StrategyName,
            Description = "100% Free - No payment required."
        };

        foreach (var course in request.Courses)
        {
            result.ItemPrices[course.Id] = 0m;
        }

        return result;
    }
}

/// <summary>
/// Standard pricing strategy: uses Course.DiscountPrice if present, otherwise Course.Price.
/// </summary>
public class StandardPricingStrategy : IPricingStrategy
{
    public string StrategyName => "Standard Pricing Strategy";

    public bool CanApply(PricingCalculationRequest request) => true; // Fallback / default

    public PriceCalculationResult CalculatePrice(PricingCalculationRequest request)
    {
        decimal original = 0m;
        decimal final = 0m;
        var itemPrices = new Dictionary<int, decimal>();

        foreach (var c in request.Courses)
        {
            decimal itemBase = c.Price;
            decimal itemEffective = c.DiscountPrice.HasValue && c.DiscountPrice.Value < c.Price
                ? c.DiscountPrice.Value
                : c.Price;

            original += itemBase;
            final += itemEffective;
            itemPrices[c.Id] = itemEffective;
        }

        return new PriceCalculationResult
        {
            OriginalSubTotal = original,
            TotalDiscount = original - final,
            FinalTotal = Math.Max(0m, final),
            StrategyApplied = StrategyName,
            Description = "Standard catalog pricing applied.",
            ItemPrices = itemPrices
        };
    }
}

/// <summary>
/// Coupon discount pricing strategy: handles percentage or fixed amount discounts.
/// </summary>
public class CouponPricingStrategy : IPricingStrategy
{
    public string StrategyName => "Coupon Discount Strategy";

    public bool CanApply(PricingCalculationRequest request)
    {
        return request.Coupon != null && request.Coupon.IsActive &&
               (!request.Coupon.ExpiryDate.HasValue || request.Coupon.ExpiryDate.Value >= DateTime.UtcNow) &&
               (!request.Coupon.UsageLimit.HasValue || request.Coupon.TimesUsed < request.Coupon.UsageLimit.Value);
    }

    public PriceCalculationResult CalculatePrice(PricingCalculationRequest request)
    {
        var coupon = request.Coupon!;
        var standard = new StandardPricingStrategy().CalculatePrice(request);
        decimal subTotal = standard.FinalTotal;

        if (coupon.MinimumSpend.HasValue && subTotal < coupon.MinimumSpend.Value)
        {
            // Coupon minimum spend not met; fallback to standard pricing with note
            return new PriceCalculationResult
            {
                OriginalSubTotal = standard.OriginalSubTotal,
                TotalDiscount = standard.TotalDiscount,
                FinalTotal = standard.FinalTotal,
                StrategyApplied = "Coupon Minimum Not Met",
                Description = $"Coupon requires minimum order of ${coupon.MinimumSpend.Value:F2}.",
                ItemPrices = standard.ItemPrices
            };
        }

        decimal discount = 0m;
        if (coupon.DiscountType == DiscountType.Percentage)
        {
            discount = subTotal * (coupon.DiscountValue / 100m);
        }
        else // Fixed amount
        {
            discount = coupon.DiscountValue;
        }

        if (discount > subTotal)
        {
            discount = subTotal;
        }

        decimal finalTotal = Math.Max(0m, subTotal - discount);
        decimal totalSavings = standard.TotalDiscount + discount;

        // Distribute proportionally across items
        var itemPrices = new Dictionary<int, decimal>();
        foreach (var kvp in standard.ItemPrices)
        {
            decimal proportion = subTotal > 0 ? (kvp.Value / subTotal) : 0;
            decimal itemDiscount = discount * proportion;
            itemPrices[kvp.Key] = Math.Max(0m, kvp.Value - itemDiscount);
        }

        return new PriceCalculationResult
        {
            OriginalSubTotal = standard.OriginalSubTotal,
            TotalDiscount = totalSavings,
            FinalTotal = finalTotal,
            StrategyApplied = StrategyName,
            Description = coupon.DiscountType == DiscountType.Percentage
                ? $"Coupon '{coupon.Code}' applied: {coupon.DiscountValue}% OFF!"
                : $"Coupon '{coupon.Code}' applied: ${coupon.DiscountValue:F2} OFF!",
            ItemPrices = itemPrices
        };
    }
}

/// <summary>
/// Bundle / Tiered pricing strategy: Cart volume discounts for multi-course enrollment.
/// 2 courses -> 10% off
/// 3 or more courses -> 20% off
/// </summary>
public class BundleTierPricingStrategy : IPricingStrategy
{
    public string StrategyName => "Bundle Multi-Course Strategy";

    public bool CanApply(PricingCalculationRequest request)
    {
        // Only applies if user hasn't supplied a coupon and has 2 or more courses
        return request.Coupon == null && request.Courses.Count >= 2;
    }

    public PriceCalculationResult CalculatePrice(PricingCalculationRequest request)
    {
        var standard = new StandardPricingStrategy().CalculatePrice(request);
        decimal subTotal = standard.FinalTotal;

        decimal discountPercent = request.Courses.Count >= 3 ? 0.20m : 0.10m;
        decimal bundleDiscount = subTotal * discountPercent;
        decimal finalTotal = Math.Max(0m, subTotal - bundleDiscount);

        var itemPrices = new Dictionary<int, decimal>();
        foreach (var kvp in standard.ItemPrices)
        {
            itemPrices[kvp.Key] = Math.Max(0m, kvp.Value * (1 - discountPercent));
        }

        return new PriceCalculationResult
        {
            OriginalSubTotal = standard.OriginalSubTotal,
            TotalDiscount = standard.TotalDiscount + bundleDiscount,
            FinalTotal = finalTotal,
            StrategyApplied = StrategyName,
            Description = $"Bundle Tier Discount: {(int)(discountPercent * 100)}% off for purchasing {request.Courses.Count} courses together!",
            ItemPrices = itemPrices
        };
    }
}

/// <summary>
/// Seasonal Promotion Strategy (e.g., Black Friday, New Year, Spring Sale)
/// </summary>
public class SeasonalPromotionStrategy : IPricingStrategy
{
    public string StrategyName => "Seasonal Promotion Strategy";

    public bool CanApply(PricingCalculationRequest request)
    {
        return request.Coupon == null && request.SeasonalDiscountPercentage > 0;
    }

    public PriceCalculationResult CalculatePrice(PricingCalculationRequest request)
    {
        var standard = new StandardPricingStrategy().CalculatePrice(request);
        decimal rate = request.SeasonalDiscountPercentage / 100m;
        decimal promoDiscount = standard.FinalTotal * rate;
        decimal finalTotal = Math.Max(0m, standard.FinalTotal - promoDiscount);

        var itemPrices = new Dictionary<int, decimal>();
        foreach (var kvp in standard.ItemPrices)
        {
            itemPrices[kvp.Key] = Math.Max(0m, kvp.Value * (1 - rate));
        }

        return new PriceCalculationResult
        {
            OriginalSubTotal = standard.OriginalSubTotal,
            TotalDiscount = standard.TotalDiscount + promoDiscount,
            FinalTotal = finalTotal,
            StrategyApplied = StrategyName,
            Description = $"{request.SeasonalCampaignName ?? "Seasonal Sale"}: {request.SeasonalDiscountPercentage}% OFF storewide!",
            ItemPrices = itemPrices
        };
    }
}
