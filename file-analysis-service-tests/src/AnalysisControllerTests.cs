using System;
using System.IO;
using System.Threading.Tasks;
using FileAnalysisService.Controllers;
using FileAnalysisService.Models;
using FileAnalysisService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FileAnalysisService.Tests
{
    public class AnalysisControllerTests
    {
        private readonly Mock<IFileAnalyzer> _fileAnalyzerMock;
        private readonly Mock<ILogger<AnalysisController>> _loggerMock;
        private readonly AnalysisController _controller;
        
        public AnalysisControllerTests()
        {
            _fileAnalyzerMock = new Mock<IFileAnalyzer>();
            _loggerMock = new Mock<ILogger<AnalysisController>>();
            _controller = new AnalysisController(_fileAnalyzerMock.Object, _loggerMock.Object);
        }
        
        [Fact]
        public async Task GetAnalysis_WithValidId_ReturnsOkResult()
        {
            var fileId = Guid.NewGuid();
            var analysisResponse = new AnalysisResponse
            {
                FileId = fileId,
                FileName = "test.txt",
                ParagraphCount = 3,
                WordCount = 100,
                CharacterCount = 500,
                AnalysisDate = DateTime.UtcNow,
                IsError = false
            };
            
            _fileAnalyzerMock
                .Setup(x => x.GetAnalysisAsync(fileId))
                .ReturnsAsync(analysisResponse);
            
            var result = await _controller.GetAnalysis(fileId);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<AnalysisResponse>(okResult.Value);
            Assert.Equal(fileId, returnValue.FileId);
            Assert.Equal("test.txt", returnValue.FileName);
            Assert.Equal(3, returnValue.ParagraphCount);
            Assert.Equal(100, returnValue.WordCount);
            Assert.Equal(500, returnValue.CharacterCount);
            Assert.False(returnValue.IsError);
        }
        
        [Fact]
        public async Task GetAnalysis_WithError_ReturnsBadRequest()
        {
            var fileId = Guid.NewGuid();
            var analysisResponse = new AnalysisResponse
            {
                FileId = fileId,
                FileName = "image.png",
                ParagraphCount = 0,
                WordCount = 0,
                CharacterCount = 0,
                AnalysisDate = DateTime.UtcNow,
                IsError = true,
                ErrorMessage = "File is not a text file and cannot be analyzed"
            };
            
            _fileAnalyzerMock
                .Setup(x => x.GetAnalysisAsync(fileId))
                .ReturnsAsync(analysisResponse);
            
            var result = await _controller.GetAnalysis(fileId);
            
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var returnValue = Assert.IsType<AnalysisResponse>(badRequestResult.Value);
            Assert.Equal(fileId, returnValue.FileId);
            Assert.True(returnValue.IsError);
            Assert.Contains("not a text file", returnValue.ErrorMessage);
        }
        
        [Fact]
        public async Task GetAnalysis_WhenFileNotFound_ReturnsNotFound()
        {
            var fileId = Guid.NewGuid();
            
            _fileAnalyzerMock
                .Setup(x => x.GetAnalysisAsync(fileId))
                .ThrowsAsync(new FileNotFoundException($"File with ID {fileId} not found"));
            
            var result = await _controller.GetAnalysis(fileId);
            
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains(fileId.ToString(), notFoundResult.Value.ToString());
        }
        
        [Fact]
        public async Task GetAnalysis_WhenExceptionOccurs_ReturnsInternalServerError()
        {
            var fileId = Guid.NewGuid();
            
            _fileAnalyzerMock
                .Setup(x => x.GetAnalysisAsync(fileId))
                .ThrowsAsync(new Exception("Test exception"));
            
            var result = await _controller.GetAnalysis(fileId);
            
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Contains("Test exception", statusCodeResult.Value.ToString());
        }
    }
}
