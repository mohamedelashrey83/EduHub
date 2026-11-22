using EduHub.Core.Entities;

namespace EduHub.Services.Interfaces;

public interface ICertificateService
{
    Task<Certificate> GenerateCertificateAsync(int enrollmentId);
    Task<byte[]> GenerateCertificatePdfBytesAsync(Certificate certificate);
    Task<Certificate?> VerifyCertificateAsync(string verificationCode);
    string GenerateVerificationCode();
}
