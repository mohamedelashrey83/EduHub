using EduHub.Core.Entities;
using EduHub.Core.Enums;
using EduHub.Core.Patterns.Strategy;
using Xunit;

namespace EduHub.Tests;

public class PricingStrategyTests
{
    [Fact]
    public void StandardPricingStrategy_CalculatesCorrectPrice_WithDiscounts()
    {
        // Arrange
        var strategy = new StandardPricingStrategy();
        var request = new PricingCalculationRequest
        {
            Courses = new List<Course>
            {
                new Course { Id = 1, Price = 100m, DiscountPrice = 80m },
                new Course { Id = 2, Price = 50m, DiscountPrice = null }
            }
        };

        // Act
        var result = strategy.CalculatePrice(request);

        // Assert
        Assert.Equal(150m, result.OriginalSubTotal);
        Assert.Equal(20m, result.TotalDiscount);
        Assert.Equal(130m, result.FinalTotal);
        Assert.Equal(80m, result.ItemPrices[1]);
        Assert.Equal(50m, result.ItemPrices[2]);
    }

    [Fact]
    public void CouponPricingStrategy_AppliesPercentageDiscount_Correctly()
    {
        // Arrange
        var strategy = new CouponPricingStrategy();
        var coupon = new Coupon
        {
            Code = "SAVE50",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 50m,
            IsActive = true
        };
        var request = new PricingCalculationRequest
        {
            Courses = new List<Course>
            {
                new Course { Id = 1, Price = 100m, DiscountPrice = null }
            },
            Coupon = coupon
        };

        // Act
        var canApply = strategy.CanApply(request);
        var result = strategy.CalculatePrice(request);

        // Assert
        Assert.True(canApply);
        Assert.Equal(100m, result.OriginalSubTotal);
        Assert.Equal(50m, result.FinalTotal);
        Assert.Equal(50m, result.TotalDiscount);
    }

    [Fact]
    public void BundleTierPricingStrategy_Applies20PercentDiscount_For3OrMoreCourses()
    {
        // Arrange
        var strategy = new BundleTierPricingStrategy();
        var request = new PricingCalculationRequest
        {
            Courses = new List<Course>
            {
                new Course { Id = 1, Price = 100m },
                new Course { Id = 2, Price = 100m },
                new Course { Id = 3, Price = 100m }
            }
        };

        // Act
        var canApply = strategy.CanApply(request);
        var result = strategy.CalculatePrice(request);

        // Assert
        Assert.True(canApply);
        Assert.Equal(300m, result.OriginalSubTotal);
        Assert.Equal(240m, result.FinalTotal); // 20% off $300 = $60 off -> $240
        Assert.Equal(60m, result.TotalDiscount);
    }

    [Fact]
    public void FreeCourseStrategy_ReturnsZero_WhenAllCoursesFree()
    {
        // Arrange
        var strategy = new FreeCourseStrategy();
        var request = new PricingCalculationRequest
        {
            Courses = new List<Course>
            {
                new Course { Id = 1, Price = 0m },
                new Course { Id = 2, Price = 10m, DiscountPrice = 0m }
            }
        };

        // Act
        var canApply = strategy.CanApply(request);
        var result = strategy.CalculatePrice(request);

        // Assert
        Assert.True(canApply);
        Assert.Equal(0m, result.FinalTotal);
        Assert.Equal(0m, result.TotalDiscount);
    }
}
