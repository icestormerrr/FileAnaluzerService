using System.Text;

namespace Server.Services;

public interface IFileAnalysisService
{
    Task<AnalysisResult> ProcessFileAsync(IFormFile file, CancellationToken cancellationToken);
}

/// <summary>
/// Реализует сервис анализа файлов: сохранение файлов, подсчёт строк/слов/символов,
/// сохранение результатов в локальной файловой системе.
/// </summary>
public class FileAnalysisService : IFileAnalysisService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileAnalysisService> _logger;
    private readonly string _uploadsFolder;
    private readonly string _resultsFolder;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="FileAnalysisService"/>.
    /// Создаёт папки для загрузки и результатов, если они не существуют.
    /// </summary>
    /// <param name="env">Информация о среде размещения веб-приложения.</param>
    /// <param name="logger">Логгер для записи диагностических сообщений.</param>
    public FileAnalysisService(IWebHostEnvironment env, ILogger<FileAnalysisService> logger)
    {
        _env = env;
        _logger = logger;
        _uploadsFolder = Path.Combine(_env.ContentRootPath, "Uploads");
        _resultsFolder = Path.Combine(_env.ContentRootPath, "Results");

        Directory.CreateDirectory(_uploadsFolder);
        Directory.CreateDirectory(_resultsFolder);
    }

    /// <inheritdoc />
    public async Task<AnalysisResult> ProcessFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        string uniqueFileName = GenerateUniqueFileName(file.FileName);
        string savedFilePath = Path.Combine(_uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(savedFilePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        string content = await File.ReadAllTextAsync(savedFilePath, Encoding.UTF8, cancellationToken);
        int lineCount = CountLines(content);
        int wordCount = CountWords(content);
        int charCount = content.Length;

        string resultFileName = $"{Path.GetFileNameWithoutExtension(uniqueFileName)}_analysis.txt";
        string resultFilePath = Path.Combine(_resultsFolder, resultFileName);
        string resultContent = $"Имя файла: {file.FileName}\n" +
                               $"Строк: {lineCount}, Слов: {wordCount}, Символов: {charCount}";

        await File.WriteAllTextAsync(resultFilePath, resultContent, Encoding.UTF8, cancellationToken);

        return new AnalysisResult
        {
            OriginalFileName = file.FileName,
            SavedFileName = uniqueFileName,
            LineCount = lineCount,
            WordCount = wordCount,
            CharCount = charCount,
            AnalysisFilePath = resultFilePath
        };
    }

    /// <summary>
    /// Генерирует уникальное имя файла, добавляя GUID к исходному имени.
    /// </summary>
    /// <param name="originalName">Исходное имя файла.</param>
    /// <returns>Уникальное имя файла.</returns>
    private string GenerateUniqueFileName(string originalName)
    {
        string name = Path.GetFileNameWithoutExtension(originalName);
        string ext = Path.GetExtension(originalName);
        return $"{name}_{Guid.NewGuid():N}{ext}";
    }

    /// <summary>
    /// Подсчитывает количество строк в тексте.
    /// </summary>
    /// <param name="text">Анализируемый текст.</param>
    /// <returns>Количество строк.</returns>
    private int CountLines(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return text.Split('\n').Length;
    }

    /// <summary>
    /// Подсчитывает количество слов в тексте, используя разделители (пробелы, знаки пунктуации и т.д.).
    /// </summary>
    /// <param name="text">Анализируемый текст.</param>
    /// <returns>Количество слов.</returns>
    private int CountWords(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        char[] separators =
            { ' ', '\n', '\r', '\t', '.', ',', '!', '?', ';', ':', '(', ')', '[', ']', '{', '}', '"', '\'' };
        return text.Split(separators, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}