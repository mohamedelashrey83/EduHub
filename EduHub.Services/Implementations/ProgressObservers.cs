using EduHub.Core.Entities;
using EduHub.Core.Interfaces;
using EduHub.Core.Patterns.Observer;
using EduHub.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EduHub.Services.Implementations;

public class CertificateGeneratorObserver : ICourseObserver
{
    private readonly ICertificateService _certificateService;
    private readonly ILogger<CertificateGeneratorObserver> _logger;

    public string ObserverName => "Certificate Generator Observer";

    public CertificateGeneratorObserver(
        ICertificateService certificateService,
        ILogger<CertificateGeneratorObserver> logger)
    {
        _certificateService = certificateService;
        _logger = logger;
    }

    public Task OnLessonCompletedAsync(LessonCompletedEvent evt)
    {
        // No certificate action on single lesson
        return Task.CompletedTask;
    }

    public async Task OnCourseCompletedAsync(CourseCompletionEvent evt)
    {
        try
        {
            _logger.LogInformation("[Observer] Course completed by {Student} in {Course}. Triggering Certificate Generation...",
                evt.StudentName, evt.CourseTitle);

            await _certificateService.GenerateCertificateAsync(evt.EnrollmentId);

            _logger.LogInformation("[Observer] Certificate generated successfully for Enrollment #{EnrollmentId}", evt.EnrollmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Observer] Failed to generate certificate for Enrollment #{EnrollmentId}", evt.EnrollmentId);
        }
    }
}

public class NotificationObserver : ICourseObserver
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationObserver> _logger;

    public string ObserverName => "In-App Notification Observer";

    public NotificationObserver(
        IUnitOfWork unitOfWork,
        ILogger<NotificationObserver> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task OnLessonCompletedAsync(LessonCompletedEvent evt)
    {
        try
        {
            var notif = new Notification
            {
                UserId = evt.StudentId,
                Title = "Lesson Completed",
                Message = $"Great progress! You finished '{evt.LessonTitle}' in '{evt.CourseTitle}'. Overall course progress: {evt.NewProgressPercentage:F0}%.",
                LinkUrl = $"/Learning/Watch/{evt.CourseId}",
                Type = "Lesson",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Notifications.AddAsync(notif);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Observer] Failed to save lesson notification for student {StudentId}", evt.StudentId);
        }
    }

    public async Task OnCourseCompletedAsync(CourseCompletionEvent evt)
    {
        try
        {
            // Student notification
            var studentNotif = new Notification
            {
                UserId = evt.StudentId,
                Title = "Course Completed! 🎉",
                Message = $"Congratulations! You have completed '{evt.CourseTitle}'. Your verified certificate is ready to view, download, and share on LinkedIn!",
                LinkUrl = $"/Certificate/ViewByEnrollment/{evt.EnrollmentId}",
                Type = "Certificate",
                CreatedAt = DateTime.UtcNow
            };

            // Instructor notification
            var instructorNotif = new Notification
            {
                UserId = evt.InstructorId,
                Title = "New Student Graduated! 🎓",
                Message = $"{evt.StudentName} just completed your course '{evt.CourseTitle}'!",
                LinkUrl = $"/Instructor/Courses",
                Type = "Graduation",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(studentNotif);
            await _unitOfWork.Notifications.AddAsync(instructorNotif);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Observer] Failed to save completion notification for student {StudentId}", evt.StudentId);
        }
    }
}

public class CourseProgressSubject : ICourseProgressSubject
{
    private readonly List<ICourseObserver> _observers = new();
    private readonly ILogger<CourseProgressSubject> _logger;

    public CourseProgressSubject(
        IEnumerable<ICourseObserver> observers,
        ILogger<CourseProgressSubject> logger)
    {
        _logger = logger;
        foreach (var observer in observers)
        {
            Attach(observer);
        }
    }

    public void Attach(ICourseObserver observer)
    {
        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);
            _logger.LogInformation("Attached observer: {Name}", observer.ObserverName);
        }
    }

    public void Detach(ICourseObserver observer)
    {
        _observers.Remove(observer);
    }

    public async Task NotifyLessonCompletedAsync(LessonCompletedEvent evt)
    {
        foreach (var observer in _observers)
        {
            await observer.OnLessonCompletedAsync(evt);
        }
    }

    public async Task NotifyCourseCompletedAsync(CourseCompletionEvent evt)
    {
        foreach (var observer in _observers)
        {
            await observer.OnCourseCompletedAsync(evt);
        }
    }
}
