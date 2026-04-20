using CatshrediasNewsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatshrediasNewsAPI.Controllers
{
    [ApiController]
    [Route("api/gigachad")]
    [Authorize]
    public class GigaChadAIController(IGigaChatService gigaChatService) : ControllerBase
    {
        // ? Ask : универсальный запрос к GigaChat
        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("Message is required");
            try
            {
                var answer = await gigaChatService.SendMessageAsync(request.Message);
                return Ok(new { response = answer });
            }
            catch { return StatusCode(500, new { error = "GigaChat недоступен" }); }
        }

        // ? Improve : улучшить стиль и читаемость текста
        [HttpPost("improve")]
        public Task<IActionResult> Improve([FromBody] TextRequest r) =>
            Process(r.Text, $"Улучши стиль и читаемость следующего текста статьи. Сохрани смысл и структуру, исправь неловкие формулировки, сделай текст более профессиональным. Верни только улучшенный текст без пояснений:\n\n{r.Text}");

        // ? Expand : увеличить объём текста
        [HttpPost("expand")]
        public Task<IActionResult> Expand([FromBody] TextRequest r) =>
            Process(r.Text, $"Расширь и дополни следующий текст статьи, добавив больше деталей, примеров и пояснений. Сохрани исходный стиль и тему. Верни только расширенный текст без пояснений:\n\n{r.Text}");

        // ? Shorten : уменьшить объём текста
        [HttpPost("shorten")]
        public Task<IActionResult> Shorten([FromBody] TextRequest r) =>
            Process(r.Text, $"Сократи следующий текст статьи, оставив только самое важное. Убери воду и повторения, сохрани ключевые мысли. Верни только сокращённый текст без пояснений:\n\n{r.Text}");

        // ? Spellcheck : проверить орфографию и пунктуацию
        [HttpPost("spellcheck")]
        public Task<IActionResult> Spellcheck([FromBody] TextRequest r) =>
            Process(r.Text, $"Проверь орфографию, пунктуацию и грамматику следующего текста. Исправь все ошибки, не меняя смысл и стиль. Верни только исправленный текст без пояснений и без списка ошибок:\n\n{r.Text}");

        // ? CheckRules : проверить соответствие правилам публикации
        [HttpPost("check-rules")]
        public Task<IActionResult> CheckRules([FromBody] TextRequest r) =>
            Process(r.Text, $"Проверь следующий текст статьи на соответствие правилам публикации: отсутствие дезинформации, разжигания ненависти, спама, нарушений авторских прав, личных данных третьих лиц. Дай краткий структурированный отчёт: что в порядке, что вызывает сомнения и почему. Отвечай на русском:\n\n{r.Text}");

        private async Task<IActionResult> Process(string text, string prompt)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length < 50)
                return BadRequest("Текст должен содержать не менее 50 символов");
            try
            {
                var result = await gigaChatService.SendMessageAsync(prompt);
                return Ok(new { response = result });
            }
            catch { return StatusCode(500, new { error = "GigaChat недоступен" }); }
        }
    }

    public class ChatRequest  { public string Message { get; set; } = ""; }
    public class TextRequest  { public string Text    { get; set; } = ""; }
}
