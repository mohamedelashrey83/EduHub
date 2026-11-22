using Microsoft.AspNetCore.Identity;

namespace EduHub.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? Headline { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public decimal TotalEarnings { get; set; } = 0m;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Course> AuthoredCourses { get; set; } = new List<Course>();
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<LessonNote> LessonNotes { get; set; } = new List<LessonNote>();
    public virtual ICollection<LessonBookmark> LessonBookmarks { get; set; } = new List<LessonBookmark>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
