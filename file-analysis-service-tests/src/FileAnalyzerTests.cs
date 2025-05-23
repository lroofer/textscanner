using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FileAnalysisService.Data;
using FileAnalysisService.Models;
using FileAnalysisService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace FileAnalysisService.Tests
{
    public class FileAnalyzerTests
    {
        private readonly Mock<ILogger<FileAnalyzer>> _loggerMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly FileAnalysisDbContext _dbContext;
        private readonly HttpClient _httpClient;
        private readonly Mock<HttpMessageHandler> _handlerMock;
        
        public FileAnalyzerTests()
        {
            _loggerMock = new Mock<ILogger<FileAnalyzer>>();
            
            _configMock = new Mock<IConfiguration>();
            _configMock.Setup(x => x["FileStoringService:Url"]).Returns("http://file-storing-service");
            
            var options = new DbContextOptionsBuilder<FileAnalysisDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new FileAnalysisDbContext(options);
            
            _handlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_handlerMock.Object)
            {
                BaseAddress = new Uri("http://file-storing-service")
            };
        }
        
        [Fact]
        public async Task GetAnalysisAsync_WhenCachedResultExists_ReturnsCachedResult()
        {
            var fileId = Guid.NewGuid();
            var cachedResult = new FileAnalysisResult
            {
                Id = Guid.NewGuid(),
                FileId = fileId,
                FileName = "test.txt",
                ParagraphCount = 3,
                WordCount = 100,
                CharacterCount = 500,
                CreatedAt = DateTime.UtcNow,
                IsError = false
            };
            
            _dbContext.FileAnalysisResults.Add(cachedResult);
            await _dbContext.SaveChangesAsync();
            
            var analyzer = new FileAnalyzer(_dbContext, _httpClient, _configMock.Object, _loggerMock.Object);
            
            var result = await analyzer.GetAnalysisAsync(fileId);
            
            Assert.Equal(fileId, result.FileId);
            Assert.Equal("test.txt", result.FileName);
            Assert.Equal(3, result.ParagraphCount);
            Assert.Equal(100, result.WordCount);
            Assert.Equal(500, result.CharacterCount);
            Assert.False(result.IsError);
        }
        
        [Fact]
        public async Task GetAnalysisAsync_WhenFileNotFound_ThrowsFileNotFoundException()
        {
            var fileId = Guid.NewGuid();
            
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString().Contains($"/api/files/{fileId}/metadata")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                });
            
            var analyzer = new FileAnalyzer(_dbContext, _httpClient, _configMock.Object, _loggerMock.Object);
            
            await Assert.ThrowsAsync<FileNotFoundException>(() => analyzer.GetAnalysisAsync(fileId));
        }
        
        [Fact]
        public async Task GetAnalysisAsync_WhenFileIsNotTextFile_ReturnsErrorResponse()
        {
            var fileId = Guid.NewGuid();
            var fileName = "image.png";
            
            var fileInfo = new Models.FileInfo
            {
                Id = fileId,
                FileName = fileName,
                ContentType = "image/png"
            };
            
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString().Contains($"/api/files/{fileId}/metadata")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(fileInfo), Encoding.UTF8, "application/json")
                });
            
            var analyzer = new FileAnalyzer(_dbContext, _httpClient, _configMock.Object, _loggerMock.Object);
            
            var result = await analyzer.GetAnalysisAsync(fileId);
            
            Assert.Equal(fileId, result.FileId);
            Assert.Equal(fileName, result.FileName);
            Assert.Equal(0, result.ParagraphCount);
            Assert.Equal(0, result.WordCount);
            Assert.Equal(0, result.CharacterCount);
            Assert.True(result.IsError);
            Assert.Contains("not a text file", result.ErrorMessage);
            
            var cachedResult = await _dbContext.FileAnalysisResults.FirstOrDefaultAsync(r => r.FileId == fileId);
            Assert.NotNull(cachedResult);
            Assert.True(cachedResult.IsError);
        }
        
        [Fact]
        public async Task GetAnalysisAsync_WithValidTextFile_AnalyzesAndCachesResult()
        {
            var fileId = Guid.NewGuid();
            var fileName = "test.txt";
            var fileContent = "This is a test.\n\nIt has two paragraphs and contains several words.";
            
            var fileInfo = new Models.FileInfo
            {
                Id = fileId,
                FileName = fileName,
                ContentType = "text/plain"
            };
            
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString().Contains($"/api/files/{fileId}/metadata")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(fileInfo), Encoding.UTF8, "application/json")
                });
                    
            var fileInfoWithBytes = new FileInfoWithBytes
            {
                Id = fileId,
                FileName = fileName,
                ContentType = "text/plain",
                Size = fileContent.Length,
                Bytes = Convert.ToBase64String(Encoding.UTF8.GetBytes(fileContent))
            };
            
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString().Contains($"/api/files/{fileId}/bytes")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(fileInfoWithBytes), Encoding.UTF8, "application/json")
                });
            
            var analyzer = new FileAnalyzer(_dbContext, _httpClient, _configMock.Object, _loggerMock.Object);
            
            var result = await analyzer.GetAnalysisAsync(fileId);
            
            Assert.Equal(fileId, result.FileId);
            Assert.Equal(fileName, result.FileName);
            Assert.Equal(2, result.ParagraphCount);
            
            Assert.Equal(12, result.WordCount);
            
            Assert.Equal(fileContent.Length, result.CharacterCount);
            Assert.False(result.IsError);
            
            var cachedResult = await _dbContext.FileAnalysisResults.FirstOrDefaultAsync(r => r.FileId == fileId);
            Assert.NotNull(cachedResult);
            Assert.Equal(2, cachedResult.ParagraphCount);
            Assert.Equal(12, cachedResult.WordCount);
        }
    }
}
