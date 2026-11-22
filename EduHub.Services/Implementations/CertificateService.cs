using EduHub.Core.Entities;
using EduHub.Core.Interfaces;
using EduHub.Services.Interfaces;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace EduHub.Services.Implementations;

public class CertificateService : ICertificateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CertificateService> _logger;

    public CertificateService(
        IUnitOfWork unitOfWork,
        IStorageService storageService,
        IWebHostEnvironment environment,
        ILogger<CertificateService> logger)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _environment = environment;
        _logger = logger;
    }

    public string GenerateVerificationCode()
    {
        var year = DateTime.UtcNow.Year;
        var randomPart = Guid.NewGuid().ToString("N")[..8].ToUpper();
        return $"EDU-{year}-{randomPart}";
    }

    public async Task<Certificate> GenerateCertificateAsync(int enrollmentId)
    {
        // Check if certificate already exists
        var existing = await _unitOfWork.Certificates.SingleOrDefaultAsync(c => c.EnrollmentId == enrollmentId);
        if (existing != null)
        {
            return existing;
        }

        var enrollment = await _unitOfWork.Enrollments.GetEnrollmentWithDetailsAsync(enrollmentId);
        if (enrollment == null)
        {
            throw new InvalidOperationException($"Enrollment with ID {enrollmentId} not found.");
        }

        var code = GenerateVerificationCode();
        var studentName = enrollment.Student?.FullName ?? "Valued Student";
        var courseTitle = enrollment.Course?.Title ?? "EduHub Course";
        var instructorName = enrollment.Course?.Instructor?.FullName ?? "EduHub Instructor";
        var verificationUrl = $"/Certificate/Verify/{code}";

        var cert = new Certificate
        {
            EnrollmentId = enrollmentId,
            CertificateCode = code,
            IssuedAt = DateTime.UtcNow,
            StudentName = studentName,
            CourseTitle = courseTitle,
            InstructorName = instructorName,
            VerificationUrl = verificationUrl
        };

        // Generate PDF bytes and save to storage
        var pdfBytes = await GenerateCertificatePdfBytesAsync(cert);
        using var ms = new MemoryStream(pdfBytes);
        var relativePath = await _storageService.UploadStreamAsync(ms, $"cert_{code}.pdf", "application/pdf", "certificates");
        cert.PdfPath = relativePath;

        await _unitOfWork.Certificates.AddAsync(cert);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Generated Certificate {Code} for student {Student} in course {Course}",
            code, studentName, courseTitle);

        return cert;
    }

    public Task<byte[]> GenerateCertificatePdfBytesAsync(Certificate certificate)
    {
        using var ms = new MemoryStream();
        // A4 Landscape orientation for certificate
        var document = new Document(PageSize.A4.Rotate(), 36f, 36f, 36f, 36f);
        var writer = PdfWriter.GetInstance(document, ms);

        document.Open();

        // Canvas for borders and ornamental background
        var cb = writer.DirectContentUnder;

        // Outer dark navy border
        cb.SetColorStroke(new BaseColor(15, 23, 42)); // #0F172A
        cb.SetLineWidth(8f);
        cb.Rectangle(24f, 24f, document.PageSize.Width - 48f, document.PageSize.Height - 48f);
        cb.Stroke();

        // Inner gold ornamental border
        cb.SetColorStroke(new BaseColor(217, 119, 6)); // #D97706 Gold
        cb.SetLineWidth(2.5f);
        cb.Rectangle(32f, 32f, document.PageSize.Width - 64f, document.PageSize.Height - 64f);
        cb.Stroke();

        // Fonts
        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 28, new BaseColor(15, 23, 42));
        var subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 13, new BaseColor(100, 116, 139));
        var studentFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 26, new BaseColor(2, 132, 199)); // #0284C7
        var courseFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 19, new BaseColor(30, 41, 59));
        var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 11, new BaseColor(71, 85, 105));
        var metaFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, new BaseColor(100, 116, 139));
        var metaBoldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, new BaseColor(15, 23, 42));

        // Content
        var table = new PdfPTable(1) { WidthPercentage = 100 };

        // Brand Header
        var brandCell = new PdfPCell(new Phrase("EDUHUB ONLINE LEARNING PLATFORM", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, new BaseColor(217, 119, 6))))
        {
            Border = Rectangle.NO_BORDER,
            HorizontalAlignment = Element.ALIGN_CENTER,
            PaddingTop = 20f,
            PaddingBottom = 6f
        };
        table.AddCell(brandCell);

        // Certificate Title
        var titleCell = new PdfPCell(new Phrase("CERTIFICATE OF COMPLETION", titleFont))
        {
            Border = Rectangle.NO_BORDER,
            HorizontalAlignment = Element.ALIGN_CENTER,
            PaddingBottom = 12f
        };
        table.AddCell(titleCell);

        // Subtitle
        var subCell = new PdfPCell(new Phrase("PROUDLY PRESENTED TO", subtitleFont))
        {
            Border = Rectangle.NO_BORDER,
            HorizontalAlignment = Element.ALIGN_CENTER,
            PaddingBottom = 10f
        };
        table.AddCell(subCell);

        // Student Name
        var nameCell = new PdfPCell(new Phrase(certificate.StudentName, studentFont))
        {
            Border = Rectangle.NO_BORDER,
            HorizontalAlignment = Element.ALIGN_CENTER,
            PaddingBottom = 12f
        };
        table.AddCell(nameCell);

        // Text
        var textCell = new PdfPCell(new Phrase("for successfully completing and demonstrating mastery in", bodyFont))
        {
            Border = Rectangle.NO_BORDER,
            HorizontalAlignment = Element.ALIGN_CENTER,
            PaddingBottom = 8f
        };
        table.AddCell(textCell);

        // Course Title
        var courseCell = new PdfPCell(new Phrase(certificate.CourseTitle, courseFont))
        {
            Border = Rectangle.NO_BORDER,
            HorizontalAlignment = Element.ALIGN_CENTER,
            PaddingBottom = 26f
        };
        table.AddCell(courseCell);

        document.Add(table);

        // Footer table with 3 columns: Date & Code, EduHub Seal, Instructor Signature
        var footerTable = new PdfPTable(3) { WidthPercentage = 90 };
        footerTable.SetWidths(new float[] { 35f, 30f, 35f });

        // Left: Date & Code
        var leftCell = new PdfPCell
        {
            Border = Rectangle.NO_BORDER,
            HorizontalAlignment = Element.ALIGN_LEFT
        };
        leftCell.AddElement(new Paragraph($"Issue Date: {certificate.IssuedAt:MMMM dd, yyyy}", metaFont));
        leftCell.AddElement(new Paragraph($"Certificate ID: {certificate.CertificateCode}", metaBoldFont));
        leftCell.AddElement(new Paragraph($"Verify: /Certificate/Verify/{certificate.CertificateCode}", metaFont));
        footerTable.AddCell(leftCell);

        // Center: Official Badge
        var centerCell = new PdfPCell
        {
            Border = Rectangle.NO_BORDER,
            HorizontalAlignment = Element.ALIGN_CENTER
        };
        var sealP = new Paragraph("★ OFFICIAL EDUHUB VERIFIED ★", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, new BaseColor(217, 119, 6)))
        {
            Alignment = Element.ALIGN_CENTER
        };
        centerCell.AddElement(sealP);
        footerTable.AddCell(centerCell);

        // Right: Instructor signature line
        var rightCell = new PdfPCell
        {
            Border = Rectangle.NO_BORDER,
            HorizontalAlignment = Element.ALIGN_RIGHT
        };
        var sigLine = new Paragraph("____________________________", metaFont) { Alignment = Element.ALIGN_RIGHT };
        var instrName = new Paragraph(certificate.InstructorName, metaBoldFont) { Alignment = Element.ALIGN_RIGHT };
        var instrTitle = new Paragraph("Course Instructor & Domain Expert", metaFont) { Alignment = Element.ALIGN_RIGHT };
        rightCell.AddElement(sigLine);
        rightCell.AddElement(instrName);
        rightCell.AddElement(instrTitle);
        footerTable.AddCell(rightCell);

        document.Add(footerTable);

        document.Close();
        return Task.FromResult(ms.ToArray());
    }

    public async Task<Certificate?> VerifyCertificateAsync(string verificationCode)
    {
        if (string.IsNullOrWhiteSpace(verificationCode)) return null;

        var clean = verificationCode.Trim().ToUpper();
        return await _unitOfWork.Certificates.SingleOrDefaultAsync(c => c.CertificateCode.ToUpper() == clean);
    }
}
