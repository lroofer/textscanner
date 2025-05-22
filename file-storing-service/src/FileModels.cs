namespace FileStoringService.Models;

public class FileEntity
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public string Hash { get; set; }
    public string Location { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FileUploadResponse
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public bool IsNew { get; set; }
}
