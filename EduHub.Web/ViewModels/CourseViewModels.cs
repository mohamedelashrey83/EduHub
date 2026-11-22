using System.ComponentModel.DataAnnotations;
using EduHub.Core.Entities;
using EduHub.Core.Enums;
using EduHub.Services.Interfaces;

namespace EduHub.Web.ViewModels;

public class CourseCatalogViewModel
{
    public IEnumerable<Course> Courses { get; set; } = new List<Course>();
    public IEnumerable<Category> Categories { get; set; } = new List<Category>();

    public string? SearchQuery { get; set; }
    public int? SelectedCategoryId { get; set; }
    public CourseLevel? SelectedLevel { get; set; }
    public bool? IsFree { get; set; }
    public string? SortBy { get; set; }
    public int TotalCount { get; set; }

    public HashSet<int> EnrolledCourseIds { get; set; } = new();
}

public class CourseDetailsViewModel
{
    public Course Course { get; set; } = null!;
    public CourseReviewSummaryDto ReviewsSummary { get; set; } = null!;
    public bool IsEnrolled { get; set; }
    public bool IsInstructorOfCourse { get; set; }
    public bool CanReview { get; set; }
    public Review? UserReview { get; set; }
    public Enrollment? UserEnrollment { get; set; }
    public int TotalStudentsCount { get; set; }
}

public class CreateCourseViewModel
{
    [Required]
    [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Subtitle { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? Requirements { get; set; }
    public string? WhatYouWillLearn { get; set; }

    [Required]
    [Range(0, 1000, ErrorMessage = "Price must be between 0 and 1000.")]
    public decimal Price { get; set; } = 0m;

    [Range(0, 1000, ErrorMessage = "Discount Price must be between 0 and 1000.")]
    public decimal? DiscountPrice { get; set; }

    [Required]
    public CourseLevel Level { get; set; } = CourseLevel.AllLevels;

    [Required]
    public int CategoryId { get; set; }
    public int? SubcategoryId { get; set; }

    public string? ThumbnailUrl { get; set; }
    public string? PromoVideoUrl { get; set; }
    public string Language { get; set; } = "English";

    // For file uploads
    public Microsoft.AspNetCore.Http.IFormFile? ThumbnailFile { get; set; }
    public Microsoft.AspNetCore.Http.IFormFile? PromoVideoFile { get; set; }

    public IEnumerable<Category> AvailableCategories { get; set; } = new List<Category>();
}

public class EditCourseViewModel : CreateCourseViewModel
{
    public int Id { get; set; }
    public CourseStatus Status { get; set; }
}
