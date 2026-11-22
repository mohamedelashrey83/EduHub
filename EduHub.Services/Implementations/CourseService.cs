using EduHub.Core.Entities;
using EduHub.Core.Enums;
using EduHub.Core.Interfaces;
using EduHub.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EduHub.Services.Implementations;

public class CourseService : ICourseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CourseService> _logger;

    public CourseService(IUnitOfWork unitOfWork, ILogger<CourseService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<Course>> GetCatalogCoursesAsync(
        int? categoryId = null,
        string? search = null,
        CourseLevel? level = null,
        bool? isFree = null,
        string? sortBy = null)
    {
        return await _unitOfWork.Courses.GetPublishedCoursesAsync(categoryId, search, level, isFree, sortBy);
    }

    public async Task<IEnumerable<Course>> GetFeaturedCoursesAsync(int count = 8)
    {
        return await _unitOfWork.Courses.GetFeaturedCoursesAsync(count);
    }

    public async Task<Course?> GetCourseDetailsAsync(int id)
    {
        return await _unitOfWork.Courses.GetCourseWithFullCurriculumAsync(id);
    }

    public async Task<Course?> GetCourseForCurriculumEditorAsync(int id, string instructorId)
    {
        var course = await _unitOfWork.Courses.GetCourseForEditAsync(id);
        if (course == null || course.InstructorId != instructorId) return null;
        return course;
    }

    public async Task<IEnumerable<Course>> GetInstructorCoursesAsync(string instructorId)
    {
        return await _unitOfWork.Courses.GetCoursesByInstructorAsync(instructorId);
    }

    public async Task<Course> CreateCourseAsync(Course course, string instructorId)
    {
        course.InstructorId = instructorId;
        course.CreatedAt = DateTime.UtcNow;
        course.Status = CourseStatus.Draft;

        await _unitOfWork.Courses.AddAsync(course);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created new course #{CourseId} by instructor {InstructorId}", course.Id, instructorId);
        return course;
    }

    public async Task<Course> UpdateCourseAsync(Course course, string instructorId)
    {
        var existing = await _unitOfWork.Courses.GetByIdAsync(course.Id);
        if (existing == null || existing.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You are not authorized to update this course.");

        existing.Title = course.Title;
        existing.Subtitle = course.Subtitle;
        existing.Description = course.Description;
        existing.Requirements = course.Requirements;
        existing.WhatYouWillLearn = course.WhatYouWillLearn;
        existing.Price = course.Price;
        existing.DiscountPrice = course.DiscountPrice;
        existing.Level = course.Level;
        existing.CategoryId = course.CategoryId;
        existing.SubcategoryId = course.SubcategoryId;
        existing.Language = course.Language;
        existing.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(course.ThumbnailUrl))
            existing.ThumbnailUrl = course.ThumbnailUrl;

        if (!string.IsNullOrWhiteSpace(course.PromoVideoUrl))
            existing.PromoVideoUrl = course.PromoVideoUrl;

        _unitOfWork.Courses.Update(existing);
        await _unitOfWork.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> DeleteCourseAsync(int courseId, string instructorId)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null || course.InstructorId != instructorId) return false;

        _unitOfWork.Courses.Remove(course);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<CourseSection> AddSectionAsync(int courseId, string title, int orderIndex)
    {
        var section = new CourseSection
        {
            CourseId = courseId,
            Title = title,
            OrderIndex = orderIndex
        };
        await _unitOfWork.CourseSections.AddAsync(section);
        await _unitOfWork.SaveChangesAsync();
        return section;
    }

    public async Task<CourseSection> UpdateSectionAsync(int sectionId, string title, int orderIndex)
    {
        var section = await _unitOfWork.CourseSections.GetByIdAsync(sectionId);
        if (section == null) throw new InvalidOperationException("Section not found.");

        section.Title = title;
        section.OrderIndex = orderIndex;
        _unitOfWork.CourseSections.Update(section);
        await _unitOfWork.SaveChangesAsync();
        return section;
    }

    public async Task<bool> DeleteSectionAsync(int sectionId)
    {
        var section = await _unitOfWork.CourseSections.GetByIdAsync(sectionId);
        if (section == null) return false;

        _unitOfWork.CourseSections.Remove(section);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<Lesson> AddLessonAsync(Lesson lesson)
    {
        await _unitOfWork.Lessons.AddAsync(lesson);
        await _unitOfWork.SaveChangesAsync();
        return lesson;
    }

    public async Task<Lesson> UpdateLessonAsync(Lesson lesson)
    {
        var existing = await _unitOfWork.Lessons.GetByIdAsync(lesson.Id);
        if (existing == null) throw new InvalidOperationException("Lesson not found.");

        existing.Title = lesson.Title;
        existing.Description = lesson.Description;
        existing.VideoUrl = lesson.VideoUrl;
        existing.DurationSeconds = lesson.DurationSeconds;
        existing.IsFreePreview = lesson.IsFreePreview;
        existing.ContentNotes = lesson.ContentNotes;
        existing.OrderIndex = lesson.OrderIndex;

        _unitOfWork.Lessons.Update(existing);
        await _unitOfWork.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteLessonAsync(int lessonId)
    {
        var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);
        if (lesson == null) return false;

        _unitOfWork.Lessons.Remove(lesson);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReorderCurriculumAsync(int courseId, List<SectionReorderDto> reorderList)
    {
        foreach (var secDto in reorderList)
        {
            var sec = await _unitOfWork.CourseSections.GetByIdAsync(secDto.SectionId);
            if (sec != null && sec.CourseId == courseId)
            {
                sec.OrderIndex = secDto.OrderIndex;
                _unitOfWork.CourseSections.Update(sec);

                foreach (var lesDto in secDto.Lessons)
                {
                    var les = await _unitOfWork.Lessons.GetByIdAsync(lesDto.LessonId);
                    if (les != null)
                    {
                        les.SectionId = secDto.SectionId;
                        les.OrderIndex = lesDto.OrderIndex;
                        _unitOfWork.Lessons.Update(les);
                    }
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SubmitCourseForApprovalAsync(int courseId, string instructorId)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null || course.InstructorId != instructorId) return false;

        course.Status = CourseStatus.PendingApproval;
        course.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Courses.Update(course);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PublishCourseAsync(int courseId, string instructorId)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null || course.InstructorId != instructorId) return false;

        course.Status = CourseStatus.Published;
        course.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Courses.Update(course);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
