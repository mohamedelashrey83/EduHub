using EduHub.Core.Entities;
using EduHub.Core.Enums;
using EduHub.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Data.Repositories;

public class ReviewRepository : Repository<Review>, IReviewRepository
{
    public ReviewRepository(EduHubDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Review>> GetCourseReviewsAsync(int courseId)
    {
        return await _context.Reviews
            .Include(r => r.Student)
            .Include(r => r.HelpfulVotes)
            .Where(r => r.CourseId == courseId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<double> GetAverageRatingForCourseAsync(int courseId)
    {
        var ratings = await _context.Reviews
            .Where(r => r.CourseId == courseId)
            .Select(r => r.Rating)
            .ToListAsync();

        return ratings.Any() ? Math.Round(ratings.Average(), 1) : 0.0;
    }

    public async Task<Dictionary<int, int>> GetRatingDistributionAsync(int courseId)
    {
        var reviews = await _context.Reviews
            .Where(r => r.CourseId == courseId)
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync();

        var dist = new Dictionary<int, int> { { 5, 0 }, { 4, 0 }, { 3, 0 }, { 2, 0 }, { 1, 0 } };
        foreach (var r in reviews)
        {
            if (dist.ContainsKey(r.Rating))
                dist[r.Rating] = r.Count;
        }

        return dist;
    }

    public async Task<Review?> GetUserReviewForCourseAsync(string studentId, int courseId)
    {
        return await _context.Reviews
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.CourseId == courseId);
    }
}

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(EduHubDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Order>> GetOrdersByStudentAsync(string studentId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Course)
            .Where(o => o.StudentId == studentId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderWithItemsAsync(int orderId)
    {
        return await _context.Orders
            .Include(o => o.Student)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Course)
                    .ThenInclude(c => c!.Instructor)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<decimal> GetTotalPlatformRevenueAsync()
    {
        return await _context.Orders
            .Where(o => o.PaymentStatus == PaymentStatus.Completed)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
    }
}
