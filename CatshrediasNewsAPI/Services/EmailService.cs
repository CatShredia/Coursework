using System.Net;
using System.Net.Mail;
using System.Net.Sockets;

namespace CatshrediasNewsAPI.Services;

public class EmailService(IConfiguration config) : IEmailService
{
    private readonly string _host = config["Smtp:Host"] ?? "localhost";
    private readonly int    _port = int.Parse(config["Smtp:Port"] ?? "1025");
    private readonly string _from = config["Smtp:From"] ?? "noreply@runews.local";
    private readonly string? _username = config["Smtp:Username"];
    private readonly string? _password = config["Smtp:Password"];
    private readonly bool _useSsl = bool.TryParse(config["Smtp:UseSsl"], out var useSsl) && useSsl;
    private readonly bool _preferIpv4 = !bool.TryParse(config["Smtp:PreferIpv4"], out var preferIpv4) || preferIpv4;

    public Task SendConfirmationAsync(string toEmail, string username, string token, string culture) =>
        SendAsync(toEmail, BuildConfirmation(culture, username, token));

    public Task SendPasswordResetAsync(string toEmail, string username, string token, string culture) =>
        SendAsync(toEmail, BuildPasswordReset(culture, username, token));

    private (string Subject, string Body) BuildConfirmation(string culture, string username, string token)
    {
        var confirmUrl = BuildUrl("/confirm-email", "token", token);
        if (CultureHelper.IsEnglish(culture))
        {
            return (
                "Confirm your email — Runews",
                $"""
                <div style="font-family:sans-serif;max-width:480px;margin:0 auto">
                  <h2 style="color:#1a73e8">Welcome to Runews, {username}!</h2>
                  <p>Please confirm your email to complete registration:</p>
                  <a href="{confirmUrl}"
                     style="display:inline-block;padding:12px 28px;background:#1a73e8;color:#fff;
                            border-radius:8px;text-decoration:none;font-weight:600">
                    Confirm email
                  </a>
                  <p style="color:#888;font-size:12px;margin-top:24px">
                    This link is valid for 24 hours. If you did not sign up, you can ignore this email.
                  </p>
                </div>
                """);
        }

        if (CultureHelper.IsTatar(culture))
        {
            return (
                "Email адресыгызны раслагыз — Runews",
                $"""
                <div style="font-family:sans-serif;max-width:480px;margin:0 auto">
                  <h2 style="color:#1a73e8">Runews, {username}, рәхим итегез!</h2>
                  <p>Теркәлүне тәмамлау өчен email адресыгызны раслагыз:</p>
                  <a href="{confirmUrl}"
                     style="display:inline-block;padding:12px 28px;background:#1a73e8;color:#fff;
                            border-radius:8px;text-decoration:none;font-weight:600">
                    Email расларга
                  </a>
                  <p style="color:#888;font-size:12px;margin-top:24px">
                    Сылтама 24 сәгатькә дә килә. Теркәлмәгәнсез икән, хатны игътибарсыз калдырыгыз.
                  </p>
                </div>
                """);
        }

        return (
            "Подтвердите ваш email — Runews",
            $"""
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
            """);
    }

    private (string Subject, string Body) BuildPasswordReset(string culture, string username, string token)
    {
        var resetUrl = BuildUrl("/reset-password", "token", token);
        if (CultureHelper.IsEnglish(culture))
        {
            return (
                "Password reset — Runews",
                $"""
                <div style="font-family:sans-serif;max-width:480px;margin:0 auto">
                  <h2 style="color:#1a73e8">Password reset, {username}</h2>
                  <p>We received a request to reset the password for your account. Click below to set a new password:</p>
                  <a href="{resetUrl}"
                     style="display:inline-block;padding:12px 28px;background:#1a73e8;color:#fff;
                            border-radius:8px;text-decoration:none;font-weight:600">
                    Reset password
                  </a>
                  <p style="color:#888;font-size:12px;margin-top:24px">
                    This link is valid for 1 hour. If you did not request a reset, you can ignore this email.
                  </p>
                </div>
                """);
        }

        if (CultureHelper.IsTatar(culture))
        {
            return (
                "Серле сүзне яңадан урнаштыру — Runews",
                $"""
                <div style="font-family:sans-serif;max-width:480px;margin:0 auto">
                  <h2 style="color:#1a73e8">Серле сүзне яңадан урнаштыру, {username}</h2>
                  <p>Сезнең аккаунт өчен серле сүзне яңадан урнаштыру соралыгы килде. Яңа серле сүз кую өчен төбәгә басыгыз:</p>
                  <a href="{resetUrl}"
                     style="display:inline-block;padding:12px 28px;background:#1a73e8;color:#fff;
                            border-radius:8px;text-decoration:none;font-weight:600">
                    Серле сүзне яңарту
                  </a>
                  <p style="color:#888;font-size:12px;margin-top:24px">
                    Сылтама 1 сәгатькә дә килә. Сорау җибәрмәгәнсез икән, хатны игътибарсыз калдырыгыз.
                  </p>
                </div>
                """);
        }

        return (
            "Сброс пароля — Runews",
            $"""
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
            """);
    }

    private string BuildUrl(string path, string queryKey, string queryValue)
    {
        var baseUrl = (config["App:BaseUrl"] ?? "http://localhost:5110").TrimEnd('/');
        return $"{baseUrl}{path}?{queryKey}={Uri.EscapeDataString(queryValue)}";
    }

    private async Task SendAsync(string toEmail, (string Subject, string Body) message)
    {
        var smtpHost = ResolveSmtpHost(_host);
        using var client  = new SmtpClient(smtpHost, _port) { EnableSsl = _useSsl };

        if (!string.IsNullOrWhiteSpace(_username))
        {
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(_username, _password ?? string.Empty);
        }
        else
        {
            client.UseDefaultCredentials = true;
            client.Credentials = CredentialCache.DefaultNetworkCredentials;
        }

        using var mail = new MailMessage(_from, toEmail, message.Subject, message.Body) { IsBodyHtml = true };
        await client.SendMailAsync(mail);
    }

    private string ResolveSmtpHost(string host)
    {
        if (_useSsl)
            return host;

        if (!_preferIpv4)
            return host;

        try
        {
            var addresses = Dns.GetHostAddresses(host);
            var ipv4 = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            return ipv4?.ToString() ?? host;
        }
        catch
        {
            return host;
        }
    }
}
