using Microsoft.AspNetCore.Mvc;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileAnalysisService _analysisService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IFileAnalysisService analysisService, ILogger<FilesController> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не выбран или пустой.");

            // Дополнительно можно проверить расширение, MIME-тип и т.д.
            if (!IsTextFile(file))
                return BadRequest("Допускаются только текстовые файлы.");

            var result = await _analysisService.ProcessFileAsync(file, cancellationToken);

            // Формируем ответ в виде текста (как в задании)
            var responseText = $"Имя файла: {result.OriginalFileName}\n" +
                               $"Строк: {result.LineCount}, Слов: {result.WordCount}, Символов: {result.CharCount}";
            
            return Ok(responseText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке файла {FileName}", file?.FileName);
            return StatusCode(500, "Внутренняя ошибка сервера. Пожалуйста, попробуйте позже.");
        }
    }

    private bool IsTextFile(IFormFile file)
    {
        // Простейшая проверка по расширению (можно улучшить)
        var allowedExtensions = new[] { ".txt", ".csv", ".log", ".json", ".xml" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(ext);
    }
}