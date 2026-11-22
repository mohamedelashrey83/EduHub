using System.ComponentModel.DataAnnotations.Schema;
using EduHub.Core.Enums;

namespace EduHub.Core.Entities;

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Requirements { get; set; }
    public string? WhatYouWillLearn { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; } = 0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountPrice { get; set; }

    public CourseLevel Level { get; set; } = CourseLevel.AllLevels;
    public CourseStatus Status { get; set; } = CourseStatus.Draft;
    public string? ThumbnailUrl { get; set; }
    public string? PromoVideoUrl { get; set; }
    public string Language { get; set; } = "English";

    public string InstructorId { get; set; } = string.Empty;
    public virtual ApplicationUser? Instructor { get; set; }

    public int CategoryId { get; set; }
    public virtual Category? Category { get; set; }

    public int? SubcategoryId { get; set; }
    public virtual Category? Subcategory { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<CourseSection> Sections { get; set; } = new List<CourseSection>();
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<CourseAnnouncement> Announcements { get; set; } = new List<CourseAnnouncement>();
    public virtual ICollection<CourseQuestion> Questions { get; set; } = new List<CourseQuestion>();
    public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
