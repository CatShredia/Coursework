using CatshrediasNewsAPI.Services;

namespace CatshrediasNewsAPI.IntegrationTests.Support;

public sealed class NoOpEmailService : IEmailService
{
    public Task SendConfirmationAsync(string toEmail, string username, string token, string culture) =>
        Task.CompletedTask;

    public Task SendPasswordResetAsync(string toEmail, string username, string token, string culture) =>
        Task.CompletedTask;
}
