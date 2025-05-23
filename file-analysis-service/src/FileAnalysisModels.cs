using System;

namespace FileAnalysisService.Models;

public class FileAnalysisResult
{
    public Guid Id { get; set; }
    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int ParagraphCount { get; set; }
    public int WordCount { get; set; }
    public int CharacterCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsError { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AnalysisResponse
{
    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int ParagraphCount { get; set; }
    public int WordCount { get; set; }
    public int CharacterCount { get; set; }
    public DateTime AnalysisDate { get; set; }
    public bool IsError { get; set; }
    public string? ErrorMessage { get; set; }
}

public class FileInfo
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

public class FileInfoWithBytes
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; } = 0;
    public string Bytes { get; set; } = string.Empty;
}