namespace EduHub.Core.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; } = "bi-mortarboard";
    public int? ParentCategoryId { get; set; }

    // Navigation properties
    public virtual Category? ParentCategory { get; set; }
    public virtual ICollection<Category> Subcategories { get; set; } = new List<Category>();
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}
