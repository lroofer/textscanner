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
    public string FileName { get; set; }
    public string ContentType { get; set; }
}

public class FileInfoWithBytes
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long Size { get; set; }
    public string Bytes { get; set; }
}