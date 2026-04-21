using System.Net;
using System.Net.Mail;

namespace CatshrediasNewsAPI.Services;

public class EmailService(IConfiguration config)
{
    private readonly string _host = config["Smtp:Host"] ?? "localhost";
    private readonly int    _port = int.Parse(config["Smtp:Port"] ?? "1025");
    private readonly string _from = config["Smtp:From"] ?? "noreply@runews.local";

    public async Task SendConfirmationAsync(string toEmail, string username, string token)
    {
        var baseUrl    = config["App:BaseUrl"] ?? "http://localhost:5110";
        var confirmUrl = $"{baseUrl}/confirm-email?token={token}";

        var body = $"""
            <div style="font-family:sans-serif;max-width:480px;margin:0 auto">
              <h2 style="color:#1a73e8">Добро пожаловать в Runews, {username}!</h2>
              <p>Для завершения регистрации подтвердите ваш email:</p>
              <a href="{confirmUrl}"
                 style="display:inline-block;padding:12px 28px;background:#1a73e8;color:#fff;
                        border-radius:8px;text-decoration:none;font-weight:600">
                Подтвердить email
              </a>
              <p style="color:#888;font-size:12px;margin-top:24px">
                Ссылка действительна 24 часа. Если вы не регистрировались — просто проигнорируйте письмо.
              </p>
            </div>
            """;

        await SendAsync(toEmail, "Подтвердите ваш email — Runews", body);
    }

    public async Task SendPasswordResetAsync(string toEmail, string username, string token)
    {
        var baseUrl  = config["App:BaseUrl"] ?? "http://localhost:5110";
        var resetUrl = $"{baseUrl}/reset-password?token={token}";

        var body = $"""
            <div style="font-family:sans-serif;max-width:480px;margin:0 auto">
              <h2 style="color:#1a73e8">Сброс пароля, {username}</h2>
              <p>Мы получили запрос на сброс пароля для вашего аккаунта. Нажмите кнопку ниже, чтобы задать новый пароль:</p>
              <a href="{resetUrl}"
                 style="display:inline-block;padding:12px 28px;background:#1a73e8;color:#fff;
                        border-radius:8px;text-decoration:none;font-weight:600">
                Сбросить пароль
              </a>
              <p style="color:#888;font-size:12px;margin-top:24px">
                Ссылка действительна 1 час. Если вы не запрашивали сброс — просто проигнорируйте это письмо.
              </p>
            </div>
            """;

        await SendAsync(toEmail, "Сброс пароля — Runews", body);
    }

    private async Task SendAsync(string toEmail, string subject, string body)
    {
        using var client  = new SmtpClient(_host, _port) { EnableSsl = false, Credentials = CredentialCache.DefaultNetworkCredentials };
        using var message = new MailMessage(_from, toEmail, subject, body) { IsBodyHtml = true };
        await client.SendMailAsync(message);
    }
}
