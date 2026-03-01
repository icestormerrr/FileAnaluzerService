using Microsoft.AspNetCore.Mvc;
using Server.Services;

namespace Server.Controllers;

/// <summary>
/// Контроллер для загрузки файлов и получения результатов анализа.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileAnalysisService _analysisService;
    private readonly ILogger<FilesController> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="FilesController"/>.
    /// </summary>
    /// <param name="analysisService">Сервис анализа файлов.</param>
    /// <param name="logger">Логгер для записи диагностических сообщений.</param>
    public FilesController(IFileAnalysisService analysisService, ILogger<FilesController> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    /// <summary>
    /// Загружает файл на сервер, выполняет его анализ и возвращает результаты.
    /// </summary>
    /// <param name="file">Загружаемый файл.</param>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    /// <returns>Текстовый ответ с количеством строк, слов и символов.</returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не выбран или пустой.");

            if (!IsTextFile(file))
                return BadRequest("Допускаются только текстовые файлы.");

            AnalysisResult result = await _analysisService.ProcessFileAsync(file, cancellationToken);

            string responseText = $"Имя файла: {result.OriginalFileName}\n" +
                                  $"Строк: {result.LineCount}, Слов: {result.WordCount}, Символов: {result.CharCount}";

            return Ok(responseText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке файла {FileName}", file?.FileName);
            return StatusCode(500, "Внутренняя ошибка сервера. Пожалуйста, попробуйте позже.");
        }
    }

    /// <summary>
    /// Проверяет, является ли загруженный файл текстовым (по расширению).
    /// </summary>
    /// <param name="file">Проверяемый файл.</param>
    /// <returns>true, если расширение файла допустимо; иначе false.</returns>
    private bool IsTextFile(IFormFile file)
    {
        string[] allowedExtensions = { ".txt", ".csv", ".log", ".json", ".xml" };
        string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(ext);
    }
}