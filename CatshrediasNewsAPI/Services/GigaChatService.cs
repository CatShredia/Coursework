using System.Text;
using System.Text.Json;

namespace CatshrediasNewsAPI.Services // Замените на namespace вашего проекта
{
    public interface IGigaChatService
    {
        Task<string> SendMessageAsync(string userMessage);
    }

    public class GigaChatService : IGigaChatService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GigaChatService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        // URL API Сбера
        private const string AuthUrl = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";
        private const string ChatUrl = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

        public GigaChatService(IConfiguration configuration, ILogger<GigaChatService> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Создает HttpClientHandler, игнорирующий ошибки SSL (для dev-среды Сбера)
        /// </summary>
        private HttpClientHandler CreateInsecureHandler()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            return handler;
        }

        public async Task<string> SendMessageAsync(string userMessage)
        {
            try
            {
                // 1. Получаем настройки из конфига (User Secrets или appsettings)
                string clientId = _configuration["GigaChat:ClientId"];
                string clientSecret = _configuration["GigaChat:ClientSecret"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    throw new InvalidOperationException("GigaChat credentials are not configured.");
                }

                // 2. Получаем токен
                string token = await GetAccessTokenAsync(clientId, clientSecret);

                // 3. Отправляем сообщение
                return await SendChatRequestAsync(token, userMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обращении к GigaChat");
                throw;
            }
        }

        private async Task<string> GetAccessTokenAsync(string clientId, string clientSecret)
        {
            // Используем отдельный HttpClient для авторизации
            var handler = CreateInsecureHandler();
            using var client = new HttpClient(handler);

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            
            var request = new HttpRequestMessage(HttpMethod.Post, AuthUrl);
            request.Headers.Add("Authorization", $"Basic {credentials}");
            request.Headers.Add("RqUID", Guid.NewGuid().ToString());
            request.Content = new StringContent("scope=GIGACHAT_API_PERS", Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("access_token", out var tokenElement))
            {
                return tokenElement.GetString() ?? throw new Exception("Token is null");
            }
            
            throw new Exception("Failed to parse access_token");
        }

        private async Task<string> SendChatRequestAsync(string token, string userMessage)
        {
            var handler = CreateInsecureHandler();
            using var client = new HttpClient(handler);
            
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var requestBody = new
            {
                model = "GigaChat-Pro", // Или GigaChat
                messages = new[]
                {
                    new { role = "system", content = "Ты полезный ассистент." },
                    new { role = "user", content = userMessage }
                },
                temperature = 0.7,
                max_tokens = 500
            };

            var jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(ChatUrl, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            
            var choices = doc.RootElement.GetProperty("choices");
            var firstChoice = choices[0];
            var message = firstChoice.GetProperty("message");
            
            return message.GetProperty("content").GetString() ?? string.Empty;
        }
    }
}