namespace CatshrediasNewsAPI.Services;

public interface IEmailService
{
    Task SendConfirmationAsync(string toEmail, string username, string token, string culture);
    Task SendPasswordResetAsync(string toEmail, string username, string token, string culture);
}
