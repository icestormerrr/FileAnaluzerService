using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Services;
using Xunit;

namespace Tests;

public class FileAnalysisServiceTests : IDisposable
{
    private readonly string _testRootPath;
    private readonly FileAnalysisService _service;
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly Mock<ILogger<FileAnalysisService>> _mockLogger;

    public FileAnalysisServiceTests()
    {
        _testRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testRootPath);

        _mockEnv = new Mock<IWebHostEnvironment>();
        _mockEnv.Setup(env => env.ContentRootPath).Returns(_testRootPath);
        _mockLogger = new Mock<ILogger<FileAnalysisService>>();

        _service = new FileAnalysisService(_mockEnv.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRootPath))
            Directory.Delete(_testRootPath, true);
    }

    [Fact]
    public async Task ProcessFileAsync_ValidTextFile_ReturnsCorrectAnalysis()
    {
        var fileName = "test.txt";
        var fileContent = "Hello world!\nThis is a test file.\nThird line.";
        var file = CreateIFormFile(fileName, fileContent);

        var result = await _service.ProcessFileAsync(file, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(fileName, result.OriginalFileName);
        Assert.Equal(3, result.LineCount);
        Assert.Equal(9, result.WordCount);
        Assert.Equal(fileContent.Length, result.CharCount);
        Assert.Contains("_analysis.txt", result.AnalysisFilePath);
        Assert.True(File.Exists(result.AnalysisFilePath));
    }

    [Fact]
    public async Task ProcessFileAsync_EmptyFile_ReturnsZeroCounts()
    {
        var fileName = "empty.txt";
        var fileContent = "";
        var file = CreateIFormFile(fileName, fileContent);

        var result = await _service.ProcessFileAsync(file, CancellationToken.None);

        Assert.Equal(0, result.LineCount);
        Assert.Equal(0, result.WordCount);
        Assert.Equal(0, result.CharCount);
    }

    [Fact]
    public async Task ProcessFileAsync_FileWithSpecialCharacters_CountsWordsCorrectly()
    {
        var fileName = "special.txt";
        var fileContent = "Hello, world! (test) [123] {abc} 'quote'";
        var file = CreateIFormFile(fileName, fileContent);

        var result = await _service.ProcessFileAsync(file, CancellationToken.None);

        Assert.Equal(1, result.LineCount);
        Assert.Equal(6, result.WordCount);
    }

    [Fact]
    public async Task ProcessFileAsync_SavesFileWithUniqueName()
    {
        var fileName = "unique.txt";
        var fileContent = "Some content";
        var file = CreateIFormFile(fileName, fileContent);

        var result1 = await _service.ProcessFileAsync(file, CancellationToken.None);
        var result2 = await _service.ProcessFileAsync(file, CancellationToken.None);

        Assert.NotEqual(result1.SavedFileName, result2.SavedFileName);
        var uploadsDir = Path.Combine(_testRootPath, "Uploads");
        var files = Directory.GetFiles(uploadsDir, "unique_*.txt");
        Assert.Equal(2, files.Length);
    }

    [Fact]
    public async Task ProcessFileAsync_CreatesAnalysisFileWithCorrectContent()
    {
        var fileName = "analysis.txt";
        var fileContent = "Line1\nLine2\n";
        var file = CreateIFormFile(fileName, fileContent);

        var result = await _service.ProcessFileAsync(file, CancellationToken.None);

        Assert.True(File.Exists(result.AnalysisFilePath));
        var analysisContent = await File.ReadAllTextAsync(result.AnalysisFilePath);
        Assert.Contains($"Имя файла: {fileName}", analysisContent);
        // Ожидаем: строк = 3 (т.к. последний \n создаёт пустую строку), слов = 2, символов = 12
        Assert.Contains("Строк: 3, Слов: 2, Символов: 12", analysisContent);
    }

    private IFormFile CreateIFormFile(string fileName, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };
        return file;
    }
}