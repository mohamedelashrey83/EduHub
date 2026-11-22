using EduHub.Core.Entities;
using EduHub.Core.Interfaces;
using EduHub.Core.Patterns.Strategy;
using EduHub.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EduHub.Services.Implementations;

public class PricingService : IPricingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PricingService> _logger;
    private readonly List<IPricingStrategy> _strategies;

    public PricingService(IUnitOfWork unitOfWork, ILogger<PricingService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;

        // Order of precedence:
        // 1. Free Courses
        // 2. Specific Coupon
        // 3. Multi-course Bundle discount
        // 4. Seasonal Sale
        // 5. Standard catalog price
        _strategies = new List<IPricingStrategy>
        {
            new FreeCourseStrategy(),
            new CouponPricingStrategy(),
            new BundleTierPricingStrategy(),
            new SeasonalPromotionStrategy(),
            new StandardPricingStrategy()
        };
    }

    public PriceCalculationResult CalculatePrice(PricingCalculationRequest request)
    {
        foreach (var strategy in _strategies)
        {
            if (strategy.CanApply(request))
            {
                _logger.LogInformation("Applying pricing strategy: {StrategyName}", strategy.StrategyName);
                return strategy.CalculatePrice(request);
            }
        }

        // Fallback default
        return new StandardPricingStrategy().CalculatePrice(request);
    }

    public async Task<Coupon?> ValidateCouponAsync(string code, decimal currentSubTotal)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        var clean = code.Trim().ToUpper();
        var coupon = await _unitOfWork.Coupons.SingleOrDefaultAsync(c => c.Code.ToUpper() == clean);

        if (coupon == null || !coupon.IsActive)
            return null;

        if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value < DateTime.UtcNow)
            return null;

        if (coupon.UsageLimit.HasValue && coupon.TimesUsed >= coupon.UsageLimit.Value)
            return null;

        if (coupon.MinimumSpend.HasValue && currentSubTotal < coupon.MinimumSpend.Value)
            return null;

        return coupon;
    }

    public IEnumerable<IPricingStrategy> GetAvailableStrategies()
    {
        return _strategies.AsReadOnly();
    }
}
