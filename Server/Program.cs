using Microsoft.AspNetCore.Http.Features;
using Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Регистрация сервисов
builder.Services.AddScoped<IFileAnalysisService, FileAnalysisService>();

// Настройка для больших файлов
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
    options.MemoryBufferThreshold = int.MaxValue;
});

var app = builder.Build();

app.MapControllers();

// Создание папок для загрузки и результатов при старте
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "Uploads");
var resultsPath = Path.Combine(app.Environment.ContentRootPath, "Results");
Directory.CreateDirectory(uploadsPath);
Directory.CreateDirectory(resultsPath);

app.Run();