using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

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
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            using var response = await _httpClient.GetAsync(
                $"{fileStoringServiceUrl}/api/files/{id}", 
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning($"File with ID {id} not found");
                return NotFound($"File with ID {id} not found");
            }
            
            response.EnsureSuccessStatusCode();
            
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var contentDisposition = response.Content.Headers.ContentDisposition;
            var fileName = contentDisposition?.FileName;
            
            if (string.IsNullOrEmpty(fileName))
            {
                var contentLocation = response.Content.Headers.ContentLocation?.ToString();
                if (!string.IsNullOrEmpty(contentLocation))
                {
                    fileName = Path.GetFileName(contentLocation);
                }
                else
                {
                    fileName = $"file_{id}";
                }
            }
            else
            {
                fileName = fileName.Trim('"');
            }
            
            _logger.LogInformation($"Streaming file {fileName} to client");
            
            var fileStream = await response.Content.ReadAsStreamAsync(cts.Token);
            return File(fileStream, contentType, fileName);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning($"Request timeout when downloading file with ID {id}");
            return StatusCode(408, "Request timeout");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"HTTP error when downloading file with ID {id}");
            return StatusCode(ex.StatusCode.HasValue ? (int)ex.StatusCode.Value : 500, 
                $"Error downloading file: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error when downloading file with ID {id}");
            return StatusCode(500, $"Unexpected error: {ex.Message}");
        }
    }

}
