using EduHub.Core.Entities;
using EduHub.Core.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace EduHub.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly EduHubDbContext _context;
    private IDbContextTransaction? _transaction;

    private ICourseRepository? _courses;
    private IEnrollmentRepository? _enrollments;
    private IReviewRepository? _reviews;
    private IOrderRepository? _orders;

    private IRepository<Category>? _categories;
    private IRepository<CourseSection>? _courseSections;
    private IRepository<Lesson>? _lessons;
    private IRepository<LessonCompletion>? _lessonCompletions;
    private IRepository<Certificate>? _certificates;
    private IRepository<Coupon>? _coupons;
    private IRepository<OrderItem>? _orderItems;
    private IRepository<Quiz>? _quizzes;
    private IRepository<QuizQuestion>? _quizQuestions;
    private IRepository<QuizAnswer>? _quizAnswers;
    private IRepository<QuizAttempt>? _quizAttempts;
    private IRepository<LessonNote>? _lessonNotes;
    private IRepository<LessonBookmark>? _lessonBookmarks;
    private IRepository<CourseAnnouncement>? _courseAnnouncements;
    private IRepository<CourseQuestion>? _courseQuestions;
    private IRepository<CourseAnswer>? _courseAnswers;
    private IRepository<Notification>? _notifications;
    private IRepository<ReviewHelpfulVote>? _reviewHelpfulVotes;
    private IRepository<ApplicationUser>? _users;

    public UnitOfWork(EduHubDbContext context)
    {
        _context = context;
    }

    public ICourseRepository Courses => _courses ??= new CourseRepository(_context);
    public IEnrollmentRepository Enrollments => _enrollments ??= new EnrollmentRepository(_context);
    public IReviewRepository Reviews => _reviews ??= new ReviewRepository(_context);
    public IOrderRepository Orders => _orders ??= new OrderRepository(_context);

    public IRepository<Category> Categories => _categories ??= new Repository<Category>(_context);
    public IRepository<CourseSection> CourseSections => _courseSections ??= new Repository<CourseSection>(_context);
    public IRepository<Lesson> Lessons => _lessons ??= new Repository<Lesson>(_context);
    public IRepository<LessonCompletion> LessonCompletions => _lessonCompletions ??= new Repository<LessonCompletion>(_context);
    public IRepository<Certificate> Certificates => _certificates ??= new Repository<Certificate>(_context);
    public IRepository<Coupon> Coupons => _coupons ??= new Repository<Coupon>(_context);
    public IRepository<OrderItem> OrderItems => _orderItems ??= new Repository<OrderItem>(_context);
    public IRepository<Quiz> Quizzes => _quizzes ??= new Repository<Quiz>(_context);
    public IRepository<QuizQuestion> QuizQuestions => _quizQuestions ??= new Repository<QuizQuestion>(_context);
    public IRepository<QuizAnswer> QuizAnswers => _quizAnswers ??= new Repository<QuizAnswer>(_context);
    public IRepository<QuizAttempt> QuizAttempts => _quizAttempts ??= new Repository<QuizAttempt>(_context);
    public IRepository<LessonNote> LessonNotes => _lessonNotes ??= new Repository<LessonNote>(_context);
    public IRepository<LessonBookmark> LessonBookmarks => _lessonBookmarks ??= new Repository<LessonBookmark>(_context);
    public IRepository<CourseAnnouncement> CourseAnnouncements => _courseAnnouncements ??= new Repository<CourseAnnouncement>(_context);
    public IRepository<CourseQuestion> CourseQuestions => _courseQuestions ??= new Repository<CourseQuestion>(_context);
    public IRepository<CourseAnswer> CourseAnswers => _courseAnswers ??= new Repository<CourseAnswer>(_context);
    public IRepository<Notification> Notifications => _notifications ??= new Repository<Notification>(_context);
    public IRepository<ReviewHelpfulVote> ReviewHelpfulVotes => _reviewHelpfulVotes ??= new Repository<ReviewHelpfulVote>(_context);
    public IRepository<ApplicationUser> Users => _users ??= new Repository<ApplicationUser>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
            }
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        try
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
            }
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
