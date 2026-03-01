using System.Text;

namespace Server.Services;

public interface IFileAnalysisService
{
    Task<AnalysisResult> ProcessFileAsync(IFormFile file, CancellationToken cancellationToken);
}

public class FileAnalysisService : IFileAnalysisService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileAnalysisService> _logger;
    private readonly string _uploadsFolder;
    private readonly string _resultsFolder;

    public FileAnalysisService(IWebHostEnvironment env, ILogger<FileAnalysisService> logger)
    {
        _env = env;
        _logger = logger;
        _uploadsFolder = Path.Combine(_env.ContentRootPath, "Uploads");
        _resultsFolder = Path.Combine(_env.ContentRootPath, "Results");
        
        Directory.CreateDirectory(_uploadsFolder);
        Directory.CreateDirectory(_resultsFolder);
    }

    public async Task<AnalysisResult> ProcessFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        // 1. Сохраняем файл с уникальным именем
        var uniqueFileName = GenerateUniqueFileName(file.FileName);
        var savedFilePath = Path.Combine(_uploadsFolder, uniqueFileName);
        
        using (var stream = new FileStream(savedFilePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }
        
        // 2. Анализируем содержимое
        var content = await File.ReadAllTextAsync(savedFilePath, Encoding.UTF8, cancellationToken);
        var lineCount = CountLines(content);
        var wordCount = CountWords(content);
        var charCount = content.Length; // включая все символы
        
        // 3. Сохраняем результат анализа
        var resultFileName = $"{Path.GetFileNameWithoutExtension(uniqueFileName)}_analysis.txt";
        var resultFilePath = Path.Combine(_resultsFolder, resultFileName);
        var resultContent = $"Имя файла: {file.FileName}\n" +
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

    private string GenerateUniqueFileName(string originalName)
    {
        var name = Path.GetFileNameWithoutExtension(originalName);
        var ext = Path.GetExtension(originalName);
        return $"{name}_{Guid.NewGuid():N}{ext}";
    }

    private int CountLines(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return text.Split('\n').Length;
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        // Простейшее разбиение по пробельным символам и знакам пунктуации
        var separators = new[] { ' ', '\n', '\r', '\t', '.', ',', '!', '?', ';', ':', '(', ')', '[', ']', '{', '}', '"', '\'' };
        return text.Split(separators, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}