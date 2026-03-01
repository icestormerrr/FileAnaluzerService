using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Server.Controllers;
using Server.Services;
using Xunit;
using Moq;

namespace Tests;

public class FilesControllerTests
    {
        private readonly Mock<IFileAnalysisService> _mockService;
        private readonly Mock<ILogger<FilesController>> _mockLogger;
        private readonly FilesController _controller;

        public FilesControllerTests()
        {
            _mockService = new Mock<IFileAnalysisService>();
            _mockLogger = new Mock<ILogger<FilesController>>();
            _controller = new FilesController(_mockService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task UploadFile_ValidFile_ReturnsOkWithAnalysisText()
        {
            // Arrange
            var file = CreateIFormFile("test.txt", "content");
            var analysisResult = new AnalysisResult
            {
                OriginalFileName = "test.txt",
                LineCount = 5,
                WordCount = 10,
                CharCount = 50
            };
            _mockService.Setup(s => s.ProcessFileAsync(file, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(analysisResult);

            // Act
            var result = await _controller.UploadFile(file, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var responseText = Assert.IsType<string>(okResult.Value);
            Assert.Contains("Имя файла: test.txt", responseText);
            Assert.Contains("Строк: 5, Слов: 10, Символов: 50", responseText);
        }

        [Fact]
        public async Task UploadFile_FileIsNull_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.UploadFile(null, CancellationToken.None);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Файл не выбран или пустой.", badRequest.Value);
        }

        [Fact]
        public async Task UploadFile_EmptyFile_ReturnsBadRequest()
        {
            // Arrange
            var file = CreateIFormFile("empty.txt", "", 0); // длина 0

            // Act
            var result = await _controller.UploadFile(file, CancellationToken.None);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Файл не выбран или пустой.", badRequest.Value);
        }

        [Fact]
        public async Task UploadFile_NonTextFileExtension_ReturnsBadRequest()
        {
            // Arrange
            var file = CreateIFormFile("image.png", "not text"); // .png не разрешён

            // Act
            var result = await _controller.UploadFile(file, CancellationToken.None);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Допускаются только текстовые файлы.", badRequest.Value);
        }

        [Fact]
        public async Task UploadFile_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var file = CreateIFormFile("test.txt", "content");
            _mockService.Setup(s => s.ProcessFileAsync(file, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new IOException("Simulated error"));

            // Act
            var result = await _controller.UploadFile(file, CancellationToken.None);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("Внутренняя ошибка сервера. Пожалуйста, попробуйте позже.", statusCodeResult.Value);

            // Проверяем, что логгер вызван с ошибкой (можно проверить через Verify, но сложно)
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        private IFormFile CreateIFormFile(string fileName, string content, long? length = null)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);
            var file = new FormFile(stream, 0, length ?? bytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };
            return file;
        }
    }