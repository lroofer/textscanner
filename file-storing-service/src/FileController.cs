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
            var (fileStream, contentType, fileName) = await _fileStorageService.GetFileAsync(id);
            return File(fileStream, contentType, fileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound($"File with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving file with ID {id}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
