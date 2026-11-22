using EduHub.Core.Interfaces;
using EduHub.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Web.Controllers;

public class CertificateController : Controller
{
    private readonly ICertificateService _certificateService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CertificateController> _logger;

    public CertificateController(
        ICertificateService certificateService,
        IUnitOfWork unitOfWork,
        ILogger<CertificateController> logger)
    {
        _certificateService = certificateService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> View(int id)
    {
        var cert = await _unitOfWork.Certificates.GetByIdAsync(id);
        if (cert == null) return NotFound("Certificate not found.");

        return View(cert);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ViewByEnrollment(int enrollmentId)
    {
        var cert = await _unitOfWork.Certificates.SingleOrDefaultAsync(c => c.EnrollmentId == enrollmentId);
        if (cert == null)
        {
            var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(enrollmentId);
            if (enrollment == null || !enrollment.IsCompleted)
            {
                TempData["ErrorMessage"] = "You must complete 100% of course lessons to earn your certificate.";
                return RedirectToAction("MyCourses", "Learning");
            }

            cert = await _certificateService.GenerateCertificateAsync(enrollmentId);
        }

        return RedirectToAction(nameof(View), new { id = cert.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var cert = await _unitOfWork.Certificates.GetByIdAsync(id);
        if (cert == null) return NotFound("Certificate not found.");

        var pdfBytes = await _certificateService.GenerateCertificatePdfBytesAsync(cert);
        return File(pdfBytes, "application/pdf", $"EduHub_Certificate_{cert.CertificateCode}.pdf");
    }

    /// <summary>
    /// Public verification portal: Anyone or employers can verify a student's certificate code!
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Verify(string? code = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return View((object?)null);
        }

        var cert = await _certificateService.VerifyCertificateAsync(code);
        ViewBag.SearchedCode = code;
        return View(cert);
    }
}
