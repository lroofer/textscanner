using FileStoringService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileStoringService.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly FileStorageService _fileStorageService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(FileStorageService fileStorageService, ILogger<FilesController> logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is empty or not provided");
        }

        try
        {
            var (response, isExisting) = await _fileStorageService.StoreFileAsync(file);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetFile(Guid id)
    {
        try
        {
            _logger.LogInformation($"Retrieving file with ID {id}");
            var (fileStream, contentType, fileName) = await _fileStorageService.GetFileAsync(id);
            
            fileStream.Position = 0;
            
            _logger.LogInformation($"Returning file {fileName}, size: {fileStream.Length} bytes, content type: {contentType}");
            
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            fileStream.Dispose();
            
            memoryStream.Position = 0;
            
            return File(memoryStream, contentType, fileName);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex.Message);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving file with ID {id}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }


}
