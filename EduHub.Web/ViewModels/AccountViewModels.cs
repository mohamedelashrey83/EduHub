using System.ComponentModel.DataAnnotations;

namespace EduHub.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Teach on EduHub (Register as Instructor)")]
    public bool IsInstructor { get; set; }

    [Display(Name = "Professional Headline")]
    public string? Headline { get; set; }
}

public class ProfileViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [Display(Name = "Professional Headline")]
    public string? Headline { get; set; }

    public string? Bio { get; set; }

    [Display(Name = "Website URL")]
    public string? WebsiteUrl { get; set; }

    [Display(Name = "LinkedIn Profile")]
    public string? LinkedInUrl { get; set; }

    public string? AvatarUrl { get; set; }
    public decimal TotalEarnings { get; set; }
    public List<string> Roles { get; set; } = new();
}
