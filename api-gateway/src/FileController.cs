using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api")]
public class FileController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FileController> _logger;
    private readonly IConfiguration _configuration;

    public FileController(IHttpClientFactory httpClientFactory, ILogger<FileController> logger, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("upload-file")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is empty or not provided");
        }

        try
        {
            var fileStoringServiceUrl = _configuration["FileStoringService:Url"];
            if (string.IsNullOrEmpty(fileStoringServiceUrl))
            {
                throw new InvalidOperationException("File Storing Service URL is not configured - check docker-compose");
            }

            using var content = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();
            using var streamContent = new StreamContent(fileStream);

            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
            content.Add(streamContent, "file", file.FileName);

            _logger.LogInformation($"Sending file {file.FileName} to {fileStoringServiceUrl}/api/files");

            var response = await _httpClient.PostAsync($"{fileStoringServiceUrl}/api/files", content);

            _logger.LogInformation($"Received response: {response.StatusCode}");

            var responseContent = await response.Content.ReadAsStreamAsync();
            _logger.LogInformation($"Response content: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                return Ok(responseContent);
            }
            else
            {
                _logger.LogError($"Error from File Storing Service: {response.StatusCode}, {responseContent}");
                return StatusCode((int)response.StatusCode, responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while uploading file");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("download-file/{id}")]
    public async Task<IActionResult> DownloadFile(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest("Invalid file ID");
        }
        
        var fileStoringServiceUrl = _configuration["FileStoringService:Url"];
        if (string.IsNullOrEmpty(fileStoringServiceUrl))
        {
            _logger.LogError("File Storing Service URL is not configured");
            return StatusCode(500, "Server configuration error");
        }

        try
        {
            _logger.LogInformation($"Downloading file with ID {id}");
            
            var fileUrl = $"{fileStoringServiceUrl}/api/files/{id}/bytes";
            _logger.LogInformation($"Requesting file bytes from {fileUrl}");
            
            var response = await _httpClient.GetStringAsync(fileUrl);
            
            var fileInfo = JsonSerializer.Deserialize<FileInfoWithBytes>(response, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (fileInfo == null)
            {
                _logger.LogWarning("Failed to deserialize file info");
                return StatusCode(500, "Failed to process file information");
            }
            
            var fileBytes = Convert.FromBase64String(fileInfo.Bytes);
            
            _logger.LogInformation($"Received file {fileInfo.FileName}, size: {fileBytes.Length} bytes");
            
            if (fileBytes.Length == 0)
            {
                _logger.LogWarning("File content is empty");
                return StatusCode(500, "File content is empty");
            }
            
            return File(fileBytes, fileInfo.ContentType, fileInfo.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading file with ID {id}");
            return StatusCode(500, $"Error downloading file: {ex.Message}");
        }
    }

    [HttpGet("analyze-file/{id}")]
    public async Task<IActionResult> AnalyzeFile(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest("Invalid file ID");
        }
        
        var fileAnalysisServiceUrl = _configuration["FileAnalysisService:Url"];
        if (string.IsNullOrEmpty(fileAnalysisServiceUrl))
        {
            _logger.LogError("File Analysis Service URL is not configured");
            return StatusCode(500, "Server configuration error");
        }

        try
        {
            _logger.LogInformation($"Requesting analysis for file with ID {id}");
            
            // Формируем URL для запроса анализа
            var analysisUrl = $"{fileAnalysisServiceUrl}/api/get_analysis/{id}";
            _logger.LogInformation($"Requesting analysis from {analysisUrl}");
            
            // Отправляем запрос
            var response = await _httpClient.GetAsync(analysisUrl);
            
            // Получаем содержимое ответа
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Проверяем успешность запроса
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Analysis service returned status code: {response.StatusCode}, Content: {responseContent}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound($"File with ID {id} not found");
                }
                
                return StatusCode((int)response.StatusCode, responseContent);
            }
            
            // Возвращаем результаты анализа
            return Ok(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error analyzing file with ID {id}");
            return StatusCode(500, $"Error analyzing file: {ex.Message}");
        }
    }

    public class FileInfoWithBytes
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Bytes { get; set; } = string.Empty;
    }
}
