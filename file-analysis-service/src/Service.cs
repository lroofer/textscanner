using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FileAnalysisService.Data;
using FileAnalysisService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FileAnalysisService.Services;

public class FileAnalyzer
{
    private readonly FileAnalysisDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileAnalyzer> _logger;

    public FileAnalyzer(FileAnalysisDbContext dbContext, HttpClient httpClient, 
                        IConfiguration configuration, ILogger<FileAnalyzer> logger)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AnalysisResponse> GetAnalysisAsync(Guid fileId)
    {
        try
        {
            _logger.LogInformation($"Getting analysis for file ID {fileId}");
            
            var cachedResult = await _dbContext.FileAnalysisResults
                .FirstOrDefaultAsync(r => r.FileId == fileId);
            
            if (cachedResult != null)
            {
                _logger.LogInformation($"Found cached analysis for file ID {fileId}");
                return new AnalysisResponse
                {
                    FileId = cachedResult.FileId,
                    FileName = cachedResult.FileName,
                    ParagraphCount = cachedResult.ParagraphCount,
                    WordCount = cachedResult.WordCount,
                    CharacterCount = cachedResult.CharacterCount,
                    AnalysisDate = cachedResult.CreatedAt,
                    IsError = cachedResult.IsError,
                    ErrorMessage = cachedResult.ErrorMessage
                };
            }
            
            var fileStoringServiceUrl = _configuration["FileStoringService:Url"];
            if (string.IsNullOrEmpty(fileStoringServiceUrl))
            {
                throw new InvalidOperationException("File Storing Service URL is not configured");
            }
            
            var metadataUrl = $"{fileStoringServiceUrl}/api/files/{fileId}/metadata";
            _logger.LogInformation($"Requesting metadata from {metadataUrl}");
            
            var metadataResponse = await _httpClient.GetAsync(metadataUrl);
            if (!metadataResponse.IsSuccessStatusCode)
            {
                if (metadataResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new FileNotFoundException($"File with ID {fileId} not found");
                }
                
                throw new HttpRequestException($"Error getting file metadata: {metadataResponse.StatusCode}");
            }
            
            var metadataString = await metadataResponse.Content.ReadAsStringAsync();
            var metadata = JsonSerializer.Deserialize<Models.FileInfo>(metadataString, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (metadata == null)
            {
                throw new JsonException("Failed to deserialize file metadata");
            }
            
            if (!IsTextFile(metadata.ContentType, metadata.FileName))
            {
                var errorResult = new FileAnalysisResult
                {
                    Id = Guid.NewGuid(),
                    FileId = fileId,
                    FileName = metadata.FileName,
                    ParagraphCount = 0,
                    WordCount = 0,
                    CharacterCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    IsError = true,
                    ErrorMessage = "File is not a text file and cannot be analyzed"
                };
                
                _dbContext.FileAnalysisResults.Add(errorResult);
                await _dbContext.SaveChangesAsync();
                
                return new AnalysisResponse
                {
                    FileId = errorResult.FileId,
                    FileName = errorResult.FileName,
                    ParagraphCount = 0,
                    WordCount = 0,
                    CharacterCount = 0,
                    AnalysisDate = errorResult.CreatedAt,
                    IsError = true,
                    ErrorMessage = errorResult.ErrorMessage
                };
            }
            
            var contentUrl = $"{fileStoringServiceUrl}/api/files/{fileId}/direct-download";
            _logger.LogInformation($"Requesting content from {contentUrl}");
            
            var contentResponse = await _httpClient.GetAsync(contentUrl);
            if (!contentResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error getting file content: {contentResponse.StatusCode}");
            }
            
            var fileContent = await contentResponse.Content.ReadAsStringAsync();
            
            var (paragraphCount, wordCount, charCount) = AnalyzeText(fileContent);
            
            var result = new FileAnalysisResult
            {
                Id = Guid.NewGuid(),
                FileId = fileId,
                FileName = metadata.FileName,
                ParagraphCount = paragraphCount,
                WordCount = wordCount,
                CharacterCount = charCount,
                CreatedAt = DateTime.UtcNow,
                IsError = false
            };
            
            _dbContext.FileAnalysisResults.Add(result);
            await _dbContext.SaveChangesAsync();
            
            return new AnalysisResponse
            {
                FileId = result.FileId,
                FileName = result.FileName,
                ParagraphCount = result.ParagraphCount,
                WordCount = result.WordCount,
                CharacterCount = result.CharacterCount,
                AnalysisDate = result.CreatedAt,
                IsError = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error analyzing file with ID {fileId}");
            
            try
            {
                var errorResult = new FileAnalysisResult
                {
                    Id = Guid.NewGuid(),
                    FileId = fileId,
                    FileName = "Unknown",
                    ParagraphCount = 0,
                    WordCount = 0,
                    CharacterCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    IsError = true,
                    ErrorMessage = ex.Message
                };
                
                _dbContext.FileAnalysisResults.Add(errorResult);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Error saving error information to database");
            }
            
            throw;
        }
    }

    private bool IsTextFile(string contentType, string fileName)
    {
        if (contentType.StartsWith("text/") || 
            contentType == "application/json" || 
            contentType == "application/xml")
        {
            return true;
        }
        
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".txt" => true,
            ".csv" => true,
            ".json" => true,
            ".xml" => true,
            ".md" => true,
            ".html" => true,
            ".htm" => true,
            ".css" => true,
            ".js" => true,
            ".ts" => true,
            ".log" => true,
            _ => false
        };
    }

    private (int ParagraphCount, int WordCount, int CharacterCount) AnalyzeText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return (0, 0, 0);
        }
        
        int charCount = text.Length;
        
        int wordCount = Regex.Matches(text, @"\b\w+\b").Count;
        
        string normalizedText = text.Replace("\r\n", "\n").Replace("\r", "\n");
        int paragraphCount = Regex.Matches(normalizedText, @"\n\s*\n").Count + 1;
        
        return (paragraphCount, wordCount, charCount);
    }
}
