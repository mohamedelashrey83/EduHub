using EduHub.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Data;

public class EduHubDbContext : IdentityDbContext<ApplicationUser>
{
    public EduHubDbContext(DbContextOptions<EduHubDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseSection> CourseSections => Set<CourseSection>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<LessonCompletion> LessonCompletions => Set<LessonCompletion>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewHelpfulVote> ReviewHelpfulVotes => Set<ReviewHelpfulVote>();
    public DbSet<LessonNote> LessonNotes => Set<LessonNote>();
    public DbSet<LessonBookmark> LessonBookmarks => Set<LessonBookmark>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CourseAnnouncement> CourseAnnouncements => Set<CourseAnnouncement>();
    public DbSet<CourseQuestion> CourseQuestions => Set<CourseQuestion>();
    public DbSet<CourseAnswer> CourseAnswers => Set<CourseAnswer>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Category hierarchy
        builder.Entity<Category>()
            .HasOne(c => c.ParentCategory)
            .WithMany(c => c.Subcategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Course configurations
        builder.Entity<Course>(entity =>
        {
            entity.HasOne(c => c.Instructor)
                .WithMany(u => u.AuthoredCourses)
                .HasForeignKey(c => c.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Category)
                .WithMany(cat => cat.Courses)
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Subcategory)
                .WithMany()
                .HasForeignKey(c => c.SubcategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(c => c.Price).HasPrecision(18, 2);
            entity.Property(c => c.DiscountPrice).HasPrecision(18, 2);
        });

        // Course Section
        builder.Entity<CourseSection>()
            .HasOne(s => s.Course)
            .WithMany(c => c.Sections)
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Lesson
        builder.Entity<Lesson>()
            .HasOne(l => l.Section)
            .WithMany(s => s.Lessons)
            .HasForeignKey(l => l.SectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enrollment
        builder.Entity<Enrollment>(entity =>
        {
            entity.HasOne(e => e.Student)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
        });

        // LessonCompletion
        builder.Entity<LessonCompletion>(entity =>
        {
            entity.HasOne(lc => lc.Enrollment)
                .WithMany(e => e.LessonCompletions)
                .HasForeignKey(lc => lc.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(lc => lc.Lesson)
                .WithMany(l => l.LessonCompletions)
                .HasForeignKey(lc => lc.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(lc => new { lc.EnrollmentId, lc.LessonId }).IsUnique();
        });

        // Certificate
        builder.Entity<Certificate>(entity =>
        {
            entity.HasOne(c => c.Enrollment)
                .WithOne(e => e.Certificate)
                .HasForeignKey<Certificate>(c => c.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => c.CertificateCode).IsUnique();
        });

        // Review
        builder.Entity<Review>(entity =>
        {
            entity.HasOne(r => r.Course)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Student)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(r => new { r.CourseId, r.StudentId }).IsUnique();
        });

        // ReviewHelpfulVote
        builder.Entity<ReviewHelpfulVote>(entity =>
        {
            entity.HasOne(v => v.Review)
                .WithMany(r => r.HelpfulVotes)
                .HasForeignKey(v => v.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(v => new { v.ReviewId, v.UserId }).IsUnique();
        });

        // Quiz Question & Answers
        builder.Entity<QuizQuestion>()
            .HasOne(q => q.Quiz)
            .WithMany(qz => qz.Questions)
            .HasForeignKey(q => q.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<QuizAnswer>()
            .HasOne(a => a.Question)
            .WithMany(q => q.Answers)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<QuizAttempt>()
            .HasOne(a => a.Quiz)
            .WithMany(q => q.Attempts)
            .HasForeignKey(a => a.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        // Order & OrderItem
        builder.Entity<Order>(entity =>
        {
            entity.HasOne(o => o.Student)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(o => o.SubTotal).HasPrecision(18, 2);
            entity.Property(o => o.DiscountAmount).HasPrecision(18, 2);
            entity.Property(o => o.TotalAmount).HasPrecision(18, 2);
        });

        builder.Entity<OrderItem>(entity =>
        {
            entity.HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(oi => oi.Course)
                .WithMany()
                .HasForeignKey(oi => oi.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(oi => oi.Price).HasPrecision(18, 2);
            entity.Property(oi => oi.DiscountApplied).HasPrecision(18, 2);
        });

        // Coupon
        builder.Entity<Coupon>(entity =>
        {
            entity.HasIndex(c => c.Code).IsUnique();
            entity.Property(c => c.DiscountValue).HasPrecision(18, 2);
            entity.Property(c => c.MinimumSpend).HasPrecision(18, 2);
        });

        // Notes & Bookmarks
        builder.Entity<LessonNote>()
            .HasOne(n => n.Student)
            .WithMany(u => u.LessonNotes)
            .HasForeignKey(n => n.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LessonBookmark>()
            .HasOne(b => b.Student)
            .WithMany(u => u.LessonBookmarks)
            .HasForeignKey(b => b.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Course Question & Answers
        builder.Entity<CourseQuestion>()
            .HasOne(q => q.Course)
            .WithMany(c => c.Questions)
            .HasForeignKey(q => q.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CourseAnswer>()
            .HasOne(a => a.Question)
            .WithMany(q => q.Answers)
            .HasForeignKey(a => a.CourseQuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
