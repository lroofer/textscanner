using System;
using System.Threading.Tasks;
using FileAnalysisService.Models;

namespace FileAnalysisService.Services
{
    public interface IFileAnalyzer
    {
        Task<AnalysisResponse> GetAnalysisAsync(Guid fileId);
    }
}
