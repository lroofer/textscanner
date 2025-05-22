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

    [HttpGet("{id}/bytes")]
    public async Task<IActionResult> GetFileBytes(Guid id)
    {
        try
        {
            _logger.LogInformation($"Retrieving file bytes for ID {id}");
            
            var fileEntity = await _dbContext.Files.FirstOrDefaultAsync(f => f.Id == id);
            if (fileEntity == null)
            {
                _logger.LogWarning($"File with ID {id} not found in database");
                return NotFound($"File with ID {id} not found");
            }
            
            var filePath = fileEntity.Location;
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning($"File {filePath} not found on disk");
                return NotFound($"File {filePath} not found on disk");
            }
            
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            _logger.LogInformation($"Read {fileBytes.Length} bytes from file {fileEntity.FileName}");
            
            return Ok(new
            {
                Id = fileEntity.Id,
                FileName = fileEntity.FileName,
                ContentType = GetContentType(fileEntity.FileName),
                Size = fileBytes.Length,
                Bytes = Convert.ToBase64String(fileBytes)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving file bytes for ID {id}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
