namespace EventParking.Business.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(
        string email,
        string fullName,
        string token);

    Task SendPasswordResetEmailAsync(
        string email,
        string fullName,
        string token);
}