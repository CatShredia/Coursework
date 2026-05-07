using System.Net;
using System.Net.Mail;
using System.Net.Sockets;

namespace CatshrediasNewsAPI.Services;

public class EmailService(IConfiguration config)
{
    private readonly string _host = config["Smtp:Host"] ?? "localhost";
    private readonly int    _port = int.Parse(config["Smtp:Port"] ?? "1025");
    private readonly string _from = config["Smtp:From"] ?? "noreply@runews.local";
    private readonly string? _username = config["Smtp:Username"];
    private readonly string? _password = config["Smtp:Password"];
    private readonly bool _useSsl = bool.TryParse(config["Smtp:UseSsl"], out var useSsl) && useSsl;
    private readonly bool _preferIpv4 = !bool.TryParse(config["Smtp:PreferIpv4"], out var preferIpv4) || preferIpv4;

    public async Task SendConfirmationAsync(string toEmail, string username, string token)
    {
        var baseUrl = (config["App:BaseUrl"] ?? "http://localhost:5110").TrimEnd('/');
        var confirmUrl = $"{baseUrl}/confirm-email?token={Uri.EscapeDataString(token)}";

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
        var baseUrl = (config["App:BaseUrl"] ?? "http://localhost:5110").TrimEnd('/');
        var resetUrl = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(token)}";

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
        var smtpHost = ResolveSmtpHost(_host);
        using var client  = new SmtpClient(smtpHost, _port)
        {
            EnableSsl = _useSsl
        };

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

        using var message = new MailMessage(_from, toEmail, subject, body) { IsBodyHtml = true };
        await client.SendMailAsync(message);
    }

    private string ResolveSmtpHost(string host)
    {
        // Для TLS SMTP (Gmail и т.п.) подключение должно идти по доменному имени,
        // иначе проверка сертификата падает с RemoteCertificateNameMismatch.
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
