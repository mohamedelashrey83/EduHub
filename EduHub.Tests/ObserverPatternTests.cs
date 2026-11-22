using EduHub.Core.Patterns.Observer;
using EduHub.Services.Implementations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EduHub.Tests;

public class ObserverPatternTests
{
    [Fact]
    public async Task CourseProgressSubject_NotifiesAttachedObservers_OnLessonCompleted()
    {
        // Arrange
        var observerMock = new Mock<ICourseObserver>();
        observerMock.Setup(o => o.ObserverName).Returns("MockObserver");

        var subject = new CourseProgressSubject(
            new List<ICourseObserver>(),
            NullLogger<CourseProgressSubject>.Instance);

        subject.Attach(observerMock.Object);

        var lessonEvt = new LessonCompletedEvent
        {
            CourseId = 1,
            LessonId = 10,
            StudentId = "student1",
            NewProgressPercentage = 50.0
        };

        // Act
        await subject.NotifyLessonCompletedAsync(lessonEvt);

        // Assert
        observerMock.Verify(o => o.OnLessonCompletedAsync(It.Is<LessonCompletedEvent>(e => e.CourseId == 1 && e.LessonId == 10)), Times.Once);
    }

    [Fact]
    public async Task CourseProgressSubject_NotifiesAttachedObservers_OnCourseCompleted()
    {
        // Arrange
        var observer1 = new Mock<ICourseObserver>();
        var observer2 = new Mock<ICourseObserver>();

        var subject = new CourseProgressSubject(
            new List<ICourseObserver> { observer1.Object, observer2.Object },
            NullLogger<CourseProgressSubject>.Instance);

        var courseEvt = new CourseCompletionEvent
        {
            EnrollmentId = 99,
            StudentId = "student1",
            StudentName = "Sarah Jenkins",
            CourseId = 1,
            CourseTitle = "ASP.NET Core Masterclass"
        };

        // Act
        await subject.NotifyCourseCompletedAsync(courseEvt);

        // Assert
        observer1.Verify(o => o.OnCourseCompletedAsync(It.Is<CourseCompletionEvent>(e => e.EnrollmentId == 99)), Times.Once);
        observer2.Verify(o => o.OnCourseCompletedAsync(It.Is<CourseCompletionEvent>(e => e.EnrollmentId == 99)), Times.Once);
    }
}
