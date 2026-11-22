using EduHub.Core.Entities;
using EduHub.Core.Patterns.Strategy;

namespace EduHub.Services.Interfaces;

public interface IPricingService
{
    PriceCalculationResult CalculatePrice(PricingCalculationRequest request);
    Task<Coupon?> ValidateCouponAsync(string code, decimal currentSubTotal);
    IEnumerable<IPricingStrategy> GetAvailableStrategies();
}
