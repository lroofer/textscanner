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

            var response = await _httpClient.PostAsync($"{fileStoringServiceUrl}/api/files", content);

            if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, responseContent);
            }

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while uploading file");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
