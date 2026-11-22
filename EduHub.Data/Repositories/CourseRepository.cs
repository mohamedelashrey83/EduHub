using EduHub.Core.Entities;
using EduHub.Core.Enums;
using EduHub.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Data.Repositories;

public class CourseRepository : Repository<Course>, ICourseRepository
{
    public CourseRepository(EduHubDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Course>> GetPublishedCoursesAsync(
        int? categoryId = null,
        string? search = null,
        CourseLevel? level = null,
        bool? isFree = null,
        string? sortBy = null)
    {
        var query = _context.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Category)
            .Include(c => c.Sections)
                .ThenInclude(s => s.Lessons)
            .Include(c => c.Reviews)
            .Where(c => c.Status == CourseStatus.Published);

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(c => c.CategoryId == categoryId.Value || (c.Category != null && c.Category.ParentCategoryId == categoryId.Value));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.Title.ToLower().Contains(s) ||
                                     (c.Subtitle != null && c.Subtitle.ToLower().Contains(s)) ||
                                     c.Description.ToLower().Contains(s));
        }

        if (level.HasValue && level.Value != CourseLevel.AllLevels)
        {
            query = query.Where(c => c.Level == level.Value);
        }

        if (isFree.HasValue)
        {
            if (isFree.Value)
            {
                query = query.Where(c => c.Price == 0 || (c.DiscountPrice.HasValue && c.DiscountPrice.Value == 0));
            }
            else
            {
                query = query.Where(c => c.Price > 0 && (!c.DiscountPrice.HasValue || c.DiscountPrice.Value > 0));
            }
        }

        query = sortBy?.ToLower() switch
        {
            "price_low_high" => query.OrderBy(c => c.DiscountPrice ?? c.Price),
            "price_high_low" => query.OrderByDescending(c => c.DiscountPrice ?? c.Price),
            "newest" => query.OrderByDescending(c => c.CreatedAt),
            "highest_rated" => query.OrderByDescending(c => c.Reviews.Any() ? c.Reviews.Average(r => r.Rating) : 0),
            "popular" => query.OrderByDescending(c => c.Enrollments.Count),
            _ => query.OrderByDescending(c => c.CreatedAt)
        };

        return await query.ToListAsync();
    }

    public async Task<Course?> GetCourseWithFullCurriculumAsync(int courseId)
    {
        return await _context.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Category)
            .Include(c => c.Sections.OrderBy(s => s.OrderIndex))
                .ThenInclude(s => s.Lessons.OrderBy(l => l.OrderIndex))
            .Include(c => c.Sections)
                .ThenInclude(s => s.Quizzes)
                    .ThenInclude(q => q.Questions)
                        .ThenInclude(qu => qu.Answers)
            .Include(c => c.Reviews)
                .ThenInclude(r => r.Student)
            .Include(c => c.Announcements)
            .FirstOrDefaultAsync(c => c.Id == courseId);
    }

    public async Task<Course?> GetCourseForEditAsync(int courseId)
    {
        return await _context.Courses
            .Include(c => c.Sections.OrderBy(s => s.OrderIndex))
                .ThenInclude(s => s.Lessons.OrderBy(l => l.OrderIndex))
            .Include(c => c.Sections)
                .ThenInclude(s => s.Quizzes)
                    .ThenInclude(q => q.Questions)
                        .ThenInclude(qu => qu.Answers)
            .FirstOrDefaultAsync(c => c.Id == courseId);
    }

    public async Task<IEnumerable<Course>> GetCoursesByInstructorAsync(string instructorId)
    {
        return await _context.Courses
            .Include(c => c.Category)
            .Include(c => c.Sections)
                .ThenInclude(s => s.Lessons)
            .Include(c => c.Enrollments)
            .Include(c => c.Reviews)
            .Where(c => c.InstructorId == instructorId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Course>> GetFeaturedCoursesAsync(int take = 8)
    {
        return await _context.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Category)
            .Include(c => c.Sections)
                .ThenInclude(s => s.Lessons)
            .Include(c => c.Reviews)
            .Where(c => c.Status == CourseStatus.Published)
            .OrderByDescending(c => c.Enrollments.Count)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<Course>> GetPendingApprovalCoursesAsync()
    {
        return await _context.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Category)
            .Include(c => c.Sections)
                .ThenInclude(s => s.Lessons)
            .Where(c => c.Status == CourseStatus.PendingApproval)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetTotalStudentsEnrolledInInstructorCoursesAsync(string instructorId)
    {
        return await _context.Enrollments
            .Where(e => e.Course != null && e.Course.InstructorId == instructorId)
            .Select(e => e.StudentId)
            .Distinct()
            .CountAsync();
    }

    public async Task<decimal> GetTotalInstructorEarningsAsync(string instructorId)
    {
        return await _context.OrderItems
            .Where(oi => oi.Course != null && oi.Course.InstructorId == instructorId && oi.Order != null && oi.Order.PaymentStatus == PaymentStatus.Completed)
            .SumAsync(oi => (decimal?)(oi.Price - oi.DiscountApplied) * 0.70m) ?? 0m; // 70% instructor share
    }
}
