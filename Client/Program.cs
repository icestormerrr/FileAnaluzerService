using System.Net.Http.Headers;

namespace Client;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();
    private const string ServerUrl = "http://localhost:5001/api/files/upload"; // или из конфига

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        string filePath;
        if (args.Length > 0)
        {
            filePath = args[0];
        }
        else
        {
            Console.Write("Введите путь к текстовому файлу: ");
            filePath = Console.ReadLine();
        }

        if (!File.Exists(filePath))
        {
            Console.WriteLine("Файл не существует.");
            return;
        }

        try
        {
            Console.WriteLine("Отправка файла на сервер...");
            var responseText = await SendFileAsync(filePath);
            Console.WriteLine("\nРезультат анализа от сервера:");
            Console.WriteLine(responseText);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    private static async Task<string> SendFileAsync(string filePath)
    {
        using var formData = new MultipartFormDataContent();
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        var fileName = Path.GetFileName(filePath);
        formData.Add(fileContent, "file", fileName);

        var response = await httpClient.PostAsync(ServerUrl, formData);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}