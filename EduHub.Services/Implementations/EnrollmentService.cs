using EduHub.Core.Entities;
using EduHub.Core.Interfaces;
using EduHub.Core.Patterns.Observer;
using EduHub.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EduHub.Services.Implementations;

public class EnrollmentService : IEnrollmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICourseProgressSubject _progressSubject;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(
        IUnitOfWork unitOfWork,
        ICourseProgressSubject progressSubject,
        ILogger<EnrollmentService> logger)
    {
        _unitOfWork = unitOfWork;
        _progressSubject = progressSubject;
        _logger = logger;
    }

    public async Task<IEnumerable<Enrollment>> GetStudentCoursesAsync(string studentId)
    {
        return await _unitOfWork.Enrollments.GetStudentEnrollmentsAsync(studentId);
    }

    public async Task<LearningDashboardDto?> GetLearningDashboardAsync(string studentId, int courseId, int? specificLessonId = null)
    {
        var enrollment = await _unitOfWork.Enrollments.GetEnrollmentByStudentAndCourseAsync(studentId, courseId);
        if (enrollment == null || enrollment.Course == null) return null;

        var allLessons = enrollment.Course.Sections
            .OrderBy(s => s.OrderIndex)
            .SelectMany(s => s.Lessons.OrderBy(l => l.OrderIndex))
            .ToList();

        if (!allLessons.Any()) return null;

        Lesson? activeLesson = null;
        if (specificLessonId.HasValue)
        {
            activeLesson = allLessons.FirstOrDefault(l => l.Id == specificLessonId.Value);
        }

        // If not found or not specified, pick first incomplete lesson or simply first lesson
        if (activeLesson == null)
        {
            var completedIds = enrollment.LessonCompletions
                .Where(lc => lc.IsCompleted)
                .Select(lc => lc.LessonId)
                .ToHashSet();

            activeLesson = allLessons.FirstOrDefault(l => !completedIds.Contains(l.Id)) ?? allLessons.First();
        }

        var completionRecord = enrollment.LessonCompletions.FirstOrDefault(lc => lc.LessonId == activeLesson.Id);
        int lastPos = completionRecord?.LastWatchPositionSeconds ?? 0;

        var notes = await _unitOfWork.LessonNotes.FindAsync(n => n.StudentId == studentId && n.LessonId == activeLesson.Id);
        var bookmarks = await _unitOfWork.LessonBookmarks.FindAsync(b => b.StudentId == studentId && b.LessonId == activeLesson.Id);

        return new LearningDashboardDto
        {
            Enrollment = enrollment,
            Course = enrollment.Course,
            CurrentLesson = activeLesson,
            CompletedLessonIds = enrollment.LessonCompletions.Where(lc => lc.IsCompleted).Select(lc => lc.LessonId).ToList(),
            Notes = notes.OrderBy(n => n.TimestampSeconds).ToList(),
            Bookmarks = bookmarks.ToList(),
            LastWatchPositionSeconds = lastPos
        };
    }

    public async Task<double> MarkLessonCompleteAsync(string studentId, int lessonId, bool isCompleted)
    {
        var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);
        if (lesson == null)
            throw new InvalidOperationException("Lesson not found.");

        var section = await _unitOfWork.CourseSections.GetByIdAsync(lesson.SectionId);
        if (section == null) throw new InvalidOperationException("Section not found.");

        int courseId = section.CourseId;
        var enrollment = await _unitOfWork.Enrollments.GetEnrollmentByStudentAndCourseAsync(studentId, courseId);
        if (enrollment == null)
            throw new InvalidOperationException("Student is not enrolled in this course.");

        var completion = await _unitOfWork.LessonCompletions
            .SingleOrDefaultAsync(lc => lc.EnrollmentId == enrollment.Id && lc.LessonId == lessonId);

        if (completion == null)
        {
            completion = new LessonCompletion
            {
                EnrollmentId = enrollment.Id,
                LessonId = lessonId,
                IsCompleted = isCompleted,
                CompletedAt = isCompleted ? DateTime.UtcNow : null
            };
            await _unitOfWork.LessonCompletions.AddAsync(completion);
        }
        else
        {
            completion.IsCompleted = isCompleted;
            completion.CompletedAt = isCompleted ? DateTime.UtcNow : null;
            _unitOfWork.LessonCompletions.Update(completion);
        }

        await _unitOfWork.SaveChangesAsync();

        // Calculate total lessons in course
        var courseWithCurriculum = await _unitOfWork.Courses.GetCourseWithFullCurriculumAsync(courseId);
        int totalLessons = courseWithCurriculum?.Sections.SelectMany(s => s.Lessons).Count() ?? 1;

        var completedCount = await _unitOfWork.LessonCompletions
            .CountAsync(lc => lc.EnrollmentId == enrollment.Id && lc.IsCompleted);

        double progress = totalLessons > 0 ? Math.Round(((double)completedCount / totalLessons) * 100.0, 1) : 0.0;
        if (progress > 100.0) progress = 100.0;

        enrollment.ProgressPercentage = progress;
        bool wasPreviouslyCompleted = enrollment.IsCompleted;

        if (progress >= 100.0)
        {
            enrollment.IsCompleted = true;
            enrollment.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            enrollment.IsCompleted = false;
            enrollment.CompletedAt = null;
        }

        _unitOfWork.Enrollments.Update(enrollment);
        await _unitOfWork.SaveChangesAsync();

        // Trigger Observer Pattern!
        var student = await _unitOfWork.Users.GetByIdAsync(studentId);

        if (isCompleted)
        {
            await _progressSubject.NotifyLessonCompletedAsync(new LessonCompletedEvent
            {
                EnrollmentId = enrollment.Id,
                StudentId = studentId,
                StudentName = student?.FullName ?? "Student",
                CourseId = courseId,
                CourseTitle = courseWithCurriculum?.Title ?? "Course",
                LessonId = lessonId,
                LessonTitle = lesson.Title,
                NewProgressPercentage = progress
            });
        }

        if (enrollment.IsCompleted && !wasPreviouslyCompleted)
        {
            _logger.LogInformation("Student {Student} graduated course #{CourseId}! Notifying course completion observers.",
                student?.FullName, courseId);

            await _progressSubject.NotifyCourseCompletedAsync(new CourseCompletionEvent
            {
                EnrollmentId = enrollment.Id,
                StudentId = studentId,
                StudentName = student?.FullName ?? "Student",
                StudentEmail = student?.Email ?? "",
                CourseId = courseId,
                CourseTitle = courseWithCurriculum?.Title ?? "Course",
                InstructorId = courseWithCurriculum?.InstructorId ?? "",
                InstructorName = courseWithCurriculum?.Instructor?.FullName ?? "Instructor",
                CompletedAt = DateTime.UtcNow
            });
        }

        return progress;
    }

    public async Task UpdateWatchPositionAsync(string studentId, int lessonId, int seconds)
    {
        var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);
        if (lesson == null) return;

        var section = await _unitOfWork.CourseSections.GetByIdAsync(lesson.SectionId);
        if (section == null) return;

        var enrollment = await _unitOfWork.Enrollments.GetEnrollmentByStudentAndCourseAsync(studentId, section.CourseId);
        if (enrollment == null) return;

        var completion = await _unitOfWork.LessonCompletions
            .SingleOrDefaultAsync(lc => lc.EnrollmentId == enrollment.Id && lc.LessonId == lessonId);

        if (completion == null)
        {
            completion = new LessonCompletion
            {
                EnrollmentId = enrollment.Id,
                LessonId = lessonId,
                LastWatchPositionSeconds = seconds,
                IsCompleted = false
            };
            await _unitOfWork.LessonCompletions.AddAsync(completion);
        }
        else
        {
            completion.LastWatchPositionSeconds = seconds;
            _unitOfWork.LessonCompletions.Update(completion);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<LessonNote> AddNoteAsync(string studentId, int lessonId, int timestampSeconds, string content)
    {
        var note = new LessonNote
        {
            StudentId = studentId,
            LessonId = lessonId,
            TimestampSeconds = timestampSeconds,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.LessonNotes.AddAsync(note);
        await _unitOfWork.SaveChangesAsync();
        return note;
    }

    public async Task<bool> DeleteNoteAsync(int noteId, string studentId)
    {
        var note = await _unitOfWork.LessonNotes.GetByIdAsync(noteId);
        if (note == null || note.StudentId != studentId) return false;

        _unitOfWork.LessonNotes.Remove(note);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleBookmarkAsync(string studentId, int lessonId, string? note)
    {
        var bookmark = await _unitOfWork.LessonBookmarks
            .SingleOrDefaultAsync(b => b.StudentId == studentId && b.LessonId == lessonId);

        if (bookmark != null)
        {
            _unitOfWork.LessonBookmarks.Remove(bookmark);
            await _unitOfWork.SaveChangesAsync();
            return false; // Removed
        }
        else
        {
            bookmark = new LessonBookmark
            {
                StudentId = studentId,
                LessonId = lessonId,
                Note = note,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.LessonBookmarks.AddAsync(bookmark);
            await _unitOfWork.SaveChangesAsync();
            return true; // Added
        }
    }

    public async Task<IEnumerable<LessonNote>> GetNotesForLessonAsync(string studentId, int lessonId)
    {
        return await _unitOfWork.LessonNotes.FindAsync(n => n.StudentId == studentId && n.LessonId == lessonId);
    }
}
