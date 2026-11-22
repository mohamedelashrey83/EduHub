using EduHub.Core.Entities;

namespace EduHub.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ICourseRepository Courses { get; }
    IEnrollmentRepository Enrollments { get; }
    IReviewRepository Reviews { get; }
    IOrderRepository Orders { get; }

    IRepository<Category> Categories { get; }
    IRepository<CourseSection> CourseSections { get; }
    IRepository<Lesson> Lessons { get; }
    IRepository<LessonCompletion> LessonCompletions { get; }
    IRepository<Certificate> Certificates { get; }
    IRepository<Coupon> Coupons { get; }
    IRepository<OrderItem> OrderItems { get; }
    IRepository<Quiz> Quizzes { get; }
    IRepository<QuizQuestion> QuizQuestions { get; }
    IRepository<QuizAnswer> QuizAnswers { get; }
    IRepository<QuizAttempt> QuizAttempts { get; }
    IRepository<LessonNote> LessonNotes { get; }
    IRepository<LessonBookmark> LessonBookmarks { get; }
    IRepository<CourseAnnouncement> CourseAnnouncements { get; }
    IRepository<CourseQuestion> CourseQuestions { get; }
    IRepository<CourseAnswer> CourseAnswers { get; }
    IRepository<Notification> Notifications { get; }
    IRepository<ReviewHelpfulVote> ReviewHelpfulVotes { get; }
    IRepository<ApplicationUser> Users { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
