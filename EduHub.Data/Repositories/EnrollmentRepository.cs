using EduHub.Core.Entities;
using EduHub.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Data.Repositories;

public class EnrollmentRepository : Repository<Enrollment>, IEnrollmentRepository
{
    public EnrollmentRepository(EduHubDbContext context) : base(context)
    {
    }

    public async Task<Enrollment?> GetEnrollmentWithDetailsAsync(int enrollmentId)
    {
        return await _context.Enrollments
            .Include(e => e.Course)
                .ThenInclude(c => c!.Instructor)
            .Include(e => e.Course)
                .ThenInclude(c => c!.Sections.OrderBy(s => s.OrderIndex))
                    .ThenInclude(s => s.Lessons.OrderBy(l => l.OrderIndex))
            .Include(e => e.Course)
                .ThenInclude(c => c!.Sections)
                    .ThenInclude(s => s.Quizzes)
                        .ThenInclude(q => q.Questions)
            .Include(e => e.LessonCompletions)
            .Include(e => e.Certificate)
            .Include(e => e.Student)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);
    }

    public async Task<Enrollment?> GetEnrollmentByStudentAndCourseAsync(string studentId, int courseId)
    {
        return await _context.Enrollments
            .Include(e => e.Course)
                .ThenInclude(c => c!.Instructor)
            .Include(e => e.Course)
                .ThenInclude(c => c!.Sections.OrderBy(s => s.OrderIndex))
                    .ThenInclude(s => s.Lessons.OrderBy(l => l.OrderIndex))
            .Include(e => e.LessonCompletions)
            .Include(e => e.Certificate)
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);
    }

    public async Task<IEnumerable<Enrollment>> GetStudentEnrollmentsAsync(string studentId)
    {
        return await _context.Enrollments
            .Include(e => e.Course)
                .ThenInclude(c => c!.Instructor)
            .Include(e => e.Course)
                .ThenInclude(c => c!.Category)
            .Include(e => e.Certificate)
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Enrollment>> GetCourseEnrollmentsAsync(int courseId)
    {
        return await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Certificate)
            .Where(e => e.CourseId == courseId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();
    }

    public async Task<bool> IsStudentEnrolledAsync(string studentId, int courseId)
    {
        return await _context.Enrollments.AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId);
    }
}
