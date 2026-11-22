using EduHub.Core.Entities;
using EduHub.Core.Enums;

namespace EduHub.Services.Interfaces;

public class SectionReorderDto
{
    public int SectionId { get; set; }
    public int OrderIndex { get; set; }
    public List<LessonReorderDto> Lessons { get; set; } = new();
}

public class LessonReorderDto
{
    public int LessonId { get; set; }
    public int SectionId { get; set; }
    public int OrderIndex { get; set; }
}

public interface ICourseService
{
    Task<IEnumerable<Course>> GetCatalogCoursesAsync(
        int? categoryId = null,
        string? search = null,
        CourseLevel? level = null,
        bool? isFree = null,
        string? sortBy = null);

    Task<IEnumerable<Course>> GetFeaturedCoursesAsync(int count = 8);
    Task<Course?> GetCourseDetailsAsync(int id);
    Task<Course?> GetCourseForCurriculumEditorAsync(int id, string instructorId);
    Task<IEnumerable<Course>> GetInstructorCoursesAsync(string instructorId);

    Task<Course> CreateCourseAsync(Course course, string instructorId);
    Task<Course> UpdateCourseAsync(Course course, string instructorId);
    Task<bool> DeleteCourseAsync(int courseId, string instructorId);

    Task<CourseSection> AddSectionAsync(int courseId, string title, int orderIndex);
    Task<CourseSection> UpdateSectionAsync(int sectionId, string title, int orderIndex);
    Task<bool> DeleteSectionAsync(int sectionId);

    Task<Lesson> AddLessonAsync(Lesson lesson);
    Task<Lesson> UpdateLessonAsync(Lesson lesson);
    Task<bool> DeleteLessonAsync(int lessonId);

    Task<bool> ReorderCurriculumAsync(int courseId, List<SectionReorderDto> reorderList);
    Task<bool> SubmitCourseForApprovalAsync(int courseId, string instructorId);
    Task<bool> PublishCourseAsync(int courseId, string instructorId);
}
