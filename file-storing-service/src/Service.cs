using System.Security.Cryptography;
using FileStoringService.Data;
using FileStoringService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileStoringService.Services;

public class FileStorageService
{
    private readonly FileDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(FileDbContext dbContext, IConfiguration configuration, ILogger<FileStorageService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(FileUploadResponse Response, bool IsExisting)> StoreFileAsync(IFormFile file)
    {
        try
        {
            string hash = await ComputeHashAsync(file);
            var existingFile = await _dbContext.Files.FirstOrDefaultAsync(f => f.Hash == hash);
            if (existingFile != null)
            {
                _logger.LogInformation($"File with hash {hash} already exists with ID {existingFile.Id}");
                return (new FileUploadResponse
                {
                    Id = existingFile.Id,
                    FileName = existingFile.FileName,
                    IsNew = false
                }, true);
            }
            
            // Nil coalesing for local debug.
            var storagePath = _configuration["StoragePath"] ?? "storage";
            Directory.CreateDirectory(storagePath);
            
            var fileId = Guid.NewGuid();
            var fileName = file.FileName;
            var fileExtension = Path.GetExtension(fileName);
            var location = Path.Combine(storagePath, $"{fileId}{fileExtension}");
            
            using (var stream = new FileStream(location, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            var fileEntity = new FileEntity
            {
                Id = fileId,
                FileName = fileName,
                Hash = hash,
                Location = location,
                CreatedAt = DateTime.UtcNow
            };
            
            _dbContext.Files.Add(fileEntity);
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation($"New file stored with ID {fileId}");
            return (new FileUploadResponse
            {
                Id = fileId,
                FileName = fileName,
                IsNew = true
            }, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing file");
            throw;
        }
    }
    
    private async Task<string> ComputeHashAsync(IFormFile file)
    {
        using var md5 = MD5.Create();
        using var stream = file.OpenReadStream();
        var hashBytes = await md5.ComputeHashAsync(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
    
    public async Task<(Stream FileStream, string ContentType, string FileName)> GetFileAsync(Guid id)
    {
        try
        {
            var fileEntity = await _dbContext.Files.FirstOrDefaultAsync(f => f.Id == id);
            if (fileEntity == null)
            {
                _logger.LogWarning($"File with ID {id} not found in database");
                throw new FileNotFoundException($"File with ID {id} not found");
            }
            
            var filePath = fileEntity.Location;
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"File {filePath} not found on disk");
                throw new FileNotFoundException($"File {filePath} not found on disk");
            }
            
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            
            var memoryStream = new MemoryStream(fileBytes);
            
            var contentType = GetContentType(fileEntity.FileName);
            
            _logger.LogInformation($"Retrieved file {fileEntity.FileName} with ID {id}, size: {fileBytes.Length} bytes");
            
            return (memoryStream, contentType, fileEntity.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving file with ID {id}");
            throw;
        }
    }


    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".txt" => "text/plain",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }

}
