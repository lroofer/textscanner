using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
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

    /// <summary>
    /// Загрузка файла в систему
    /// </summary>
    /// <param name="file">Файл для загрузки</param>
    /// <returns>Информация о загруженном файле</returns>
    /// <remarks>
    /// Пример запроса:
    /// 
    ///     POST /api/upload-file
    ///     Content-Type: multipart/form-data
    ///     
    ///     --boundary
    ///     Content-Disposition: form-data; name="file"; filename="example.txt"
    ///     Content-Type: text/plain
    ///     
    ///     Содержимое файла
    ///     --boundary--
    /// 
    /// </remarks>
    /// <response code="200">Файл успешно загружен</response>
    /// <response code="304">Файл уже существует в системе</response>
    /// <response code="400">Некорректный запрос (файл отсутствует или пуст)</response>
    /// <response code="500">Ошибка сервера при обработке запроса</response>
    [HttpPost("upload-file")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Скачивание файла по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор файла</param>
    /// <returns>Содержимое файла</returns>
    /// <remarks>
    /// Пример запроса:
    /// 
    ///     GET /api/download-file/123e4567-e89b-12d3-a456-426614174000
    /// 
    /// </remarks>
    /// <response code="200">Файл успешно найден и возвращен</response>
    /// <response code="400">Некорректный идентификатор файла</response>
    /// <response code="404">Файл не найден</response>
    /// <response code="500">Ошибка сервера при обработке запроса</response>
    [HttpGet("download-file/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Анализ текстового файла
    /// </summary>
    /// <param name="id">Идентификатор файла</param>
    /// <returns>Результаты анализа (количество абзацев, слов, символов)</returns>
    /// <remarks>
    /// Пример запроса:
    /// 
    ///     GET /api/analyze-file/123e4567-e89b-12d3-a456-426614174000
    /// 
    /// Пример ответа:
    /// 
    ///     {
    ///        "fileId": "123e4567-e89b-12d3-a456-426614174000",
    ///        "fileName": "example.txt",
    ///        "paragraphCount": 3,
    ///        "wordCount": 50,
    ///        "characterCount": 250,
    ///        "analysisDate": "2023-05-22T12:34:56.789Z",
    ///        "isError": false
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">Анализ успешно выполнен</response>
    /// <response code="400">Некорректный идентификатор файла или файл не является текстовым</response>
    /// <response code="404">Файл не найден</response>
    /// <response code="500">Ошибка сервера при обработке запроса</response>
    [HttpGet("analyze-file/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
            
            var analysisUrl = $"{fileAnalysisServiceUrl}/api/get_analysis/{id}";
            _logger.LogInformation($"Requesting analysis from {analysisUrl}");
            
            var response = await _httpClient.GetAsync(analysisUrl);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Analysis service returned status code: {response.StatusCode}, Content: {responseContent}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound($"File with ID {id} not found");
                }
                
                return StatusCode((int)response.StatusCode, responseContent);
            }
            
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
