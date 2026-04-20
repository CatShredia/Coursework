using CatshrediasNewsAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatshrediasNewsAPI.Controllers
{
    [ApiController]
    [Route("api/gigachad/[controller]")]
    public class GigaChadAIController : ControllerBase
    {
        private readonly IGigaChatService _gigaChatService;

        public GigaChadAIController(IGigaChatService gigaChatService)
        {
            _gigaChatService = gigaChatService;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskGigaChat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Message))
            {
                return BadRequest("Message is required");
            }

            try
            {
                string answer = await _gigaChatService.SendMessageAsync(request.Message);
                return Ok(new { response = answer });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error while contacting GigaChat" });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
    }
}