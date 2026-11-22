using EduHub.Core.Entities;
using EduHub.Core.Enums;

namespace EduHub.Core.Interfaces;

public interface ICourseRepository : IRepository<Course>
{
    Task<IEnumerable<Course>> GetPublishedCoursesAsync(int? categoryId = null, string? search = null, CourseLevel? level = null, bool? isFree = null, string? sortBy = null);
    Task<Course?> GetCourseWithFullCurriculumAsync(int courseId);
    Task<Course?> GetCourseForEditAsync(int courseId);
    Task<IEnumerable<Course>> GetCoursesByInstructorAsync(string instructorId);
    Task<IEnumerable<Course>> GetFeaturedCoursesAsync(int take = 8);
    Task<IEnumerable<Course>> GetPendingApprovalCoursesAsync();
    Task<int> GetTotalStudentsEnrolledInInstructorCoursesAsync(string instructorId);
    Task<decimal> GetTotalInstructorEarningsAsync(string instructorId);
}

public interface IEnrollmentRepository : IRepository<Enrollment>
{
    Task<Enrollment?> GetEnrollmentWithDetailsAsync(int enrollmentId);
    Task<Enrollment?> GetEnrollmentByStudentAndCourseAsync(string studentId, int courseId);
    Task<IEnumerable<Enrollment>> GetStudentEnrollmentsAsync(string studentId);
    Task<IEnumerable<Enrollment>> GetCourseEnrollmentsAsync(int courseId);
    Task<bool> IsStudentEnrolledAsync(string studentId, int courseId);
}

public interface IReviewRepository : IRepository<Review>
{
    Task<IEnumerable<Review>> GetCourseReviewsAsync(int courseId);
    Task<double> GetAverageRatingForCourseAsync(int courseId);
    Task<Dictionary<int, int>> GetRatingDistributionAsync(int courseId);
    Task<Review?> GetUserReviewForCourseAsync(string studentId, int courseId);
}

public interface IOrderRepository : IRepository<Order>
{
    Task<IEnumerable<Order>> GetOrdersByStudentAsync(string studentId);
    Task<Order?> GetOrderWithItemsAsync(int orderId);
    Task<decimal> GetTotalPlatformRevenueAsync();
}
