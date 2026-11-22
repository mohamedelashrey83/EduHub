using EduHub.Core.Entities;
using EduHub.Core.Interfaces;
using EduHub.Services.Implementations;
using EduHub.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EduHub.Tests;

public class CertificateServiceTests
{
    [Fact]
    public async Task GenerateCertificatePdfBytesAsync_ReturnsValidPdfBytes()
    {
        // Arrange
        var uowMock = new Mock<IUnitOfWork>();
        var storageMock = new Mock<IStorageService>();
        var envMock = new Mock<IWebHostEnvironment>();

        var certService = new CertificateService(
            uowMock.Object,
            storageMock.Object,
            envMock.Object,
            NullLogger<CertificateService>.Instance);

        var certificate = new Certificate
        {
            CertificateCode = "EDU-2026-TESTCODE",
            StudentName = "Sarah Jenkins",
            CourseTitle = "Clean Architecture with ASP.NET Core",
            InstructorName = "Dr. Alexander Wright",
            IssuedAt = DateTime.UtcNow
        };

        // Act
        var pdfBytes = await certService.GenerateCertificatePdfBytesAsync(certificate);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 500); // Valid PDF length
        // Verify PDF Magic Bytes (%PDF-)
        Assert.Equal(0x25, pdfBytes[0]); // %
        Assert.Equal(0x50, pdfBytes[1]); // P
        Assert.Equal(0x44, pdfBytes[2]); // D
        Assert.Equal(0x46, pdfBytes[3]); // F
    }
}
