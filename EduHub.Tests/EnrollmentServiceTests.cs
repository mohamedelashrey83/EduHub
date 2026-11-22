using EduHub.Core.Entities;
using EduHub.Core.Enums;
using EduHub.Core.Patterns.Observer;
using EduHub.Data;
using EduHub.Data.Repositories;
using EduHub.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EduHub.Tests;

public class EnrollmentServiceTests
{
    private EduHubDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<EduHubDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new EduHubDbContext(options);
    }

    [Fact]
    public async Task MarkLessonComplete_SingleLessonCourse_CompletesCourseAndNotifiesObservers()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        using var uow = new UnitOfWork(context);

        var student = new ApplicationUser
        {
            Id = "student-100",
            UserName = "student100@test.com",
            FullName = "Jane Doe"
        };
        await context.Users.AddAsync(student);

        var course = new Course
        {
            Title = "Complete C# Masterclass",
            Description = "Comprehensive C# Course",
            InstructorId = student.Id,
            Price = 0m,
            Status = CourseStatus.Published
        };
        await uow.Courses.AddAsync(course);
        await uow.SaveChangesAsync();

        var section = new CourseSection
        {
            CourseId = course.Id,
            Title = "Introduction",
            OrderIndex = 1
        };
        await uow.CourseSections.AddAsync(section);
        await uow.SaveChangesAsync();

        var lesson = new Lesson
        {
            SectionId = section.Id,
            Title = "Welcome to the Course",
            OrderIndex = 1,
            DurationSeconds = 300
        };
        await uow.Lessons.AddAsync(lesson);
        await uow.SaveChangesAsync();

        var enrollment = new Enrollment
        {
            StudentId = student.Id,
            CourseId = course.Id,
            ProgressPercentage = 0,
            IsCompleted = false
        };
        await uow.Enrollments.AddAsync(enrollment);
        await uow.SaveChangesAsync();

        var observerMock = new Mock<ICourseProgressSubject>();
        var service = new EnrollmentService(uow, observerMock.Object, NullLogger<EnrollmentService>.Instance);

        // Act
        var progress = await service.MarkLessonCompleteAsync(student.Id, lesson.Id, true);

        // Assert
        Assert.Equal(100.0, progress);

        // Verify that lesson completion event was dispatched
        observerMock.Verify(s => s.NotifyLessonCompletedAsync(
            It.Is<LessonCompletedEvent>(e => e.StudentId == student.Id && e.LessonId == lesson.Id && e.NewProgressPercentage == 100.0)),
            Times.Once);

        // Verify that course completion event was dispatched because 100% was reached
        observerMock.Verify(s => s.NotifyCourseCompletedAsync(
            It.Is<CourseCompletionEvent>(e => e.StudentId == student.Id && e.CourseId == course.Id)),
            Times.Once);

        // Verify enrollment persisted in database as completed
        var updatedEnrollment = await uow.Enrollments.GetByIdAsync(enrollment.Id);
        Assert.NotNull(updatedEnrollment);
        Assert.True(updatedEnrollment.IsCompleted);
        Assert.Equal(100.0, updatedEnrollment.ProgressPercentage);
    }
}
