using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly AzureAiService _service;

    public DocumentController(AzureAiService service)
    {
        _service = service;
    }

    [HttpPost("convert")]
    public async Task<IActionResult> Convert(IFormFile file, [FromQuery] string lang = "en")
    {
        if (file == null || file.Length == 0)
            return BadRequest("Invalid file");

        using var stream = file.OpenReadStream();

        try
        {
            // Extract
            var text = await _service.ExtractTextAsync(stream);

            if (string.IsNullOrWhiteSpace(text))
                return BadRequest("No text found");

            // Translate
            var translated = lang == "en"
                ? text
                : await _service.TranslateTextAsync(text, lang);

            // Speech
            var audio = await _service.ConvertTextToSpeechAsync(translated, lang);

            return File(audio, "audio/wav", "output.wav");
        }
        catch (Exception ex)
        {
            return BadRequest($"Error: {ex.Message}");
        }
    }
}