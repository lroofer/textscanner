using System;
using System.IO;
using System.Threading.Tasks;
using FileAnalysisService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FileAnalysisService.Controllers;

[ApiController]
[Route("api")]
public class AnalysisController : ControllerBase
{
    private readonly IFileAnalyzer _fileAnalyzer;
    private readonly ILogger<AnalysisController> _logger;

    public AnalysisController(IFileAnalyzer fileAnalyzer, ILogger<AnalysisController> logger)
    {
        _fileAnalyzer = fileAnalyzer;
        _logger = logger;
    }

    [HttpGet("get_analysis/{id}")]
    public async Task<IActionResult> GetAnalysis(Guid id)
    {
        try
        {
            _logger.LogInformation($"Analysis requested for file ID {id}");
            
            var result = await _fileAnalyzer.GetAnalysisAsync(id);
            
            if (result.IsError)
            {
                _logger.LogWarning($"Analysis for file ID {id} resulted in error: {result.ErrorMessage}");
                return BadRequest(result);
            }
            
            _logger.LogInformation($"Analysis for file ID {id} completed successfully");
            return Ok(result);
        }
        catch (FileNotFoundException)
        {
            _logger.LogWarning($"File with ID {id} not found");
            return NotFound($"File with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error analyzing file with ID {id}");
            return StatusCode(500, $"Error analyzing file: {ex.Message}");
        }
    }
}
