namespace EduHub.Core.Entities;

public class Certificate
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public virtual Enrollment? Enrollment { get; set; }

    public string CertificateCode { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public string? PdfPath { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string? VerificationUrl { get; set; }
}
