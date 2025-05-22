using FileAnalysisService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileAnalysisService.Data;

public class FileAnalysisDbContext : DbContext
{
    public FileAnalysisDbContext(DbContextOptions<FileAnalysisDbContext> options) : base(options)
    {
    }

    public DbSet<FileAnalysisResult> FileAnalysisResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<FileAnalysisResult>()
            .HasKey(r => r.Id);
            
        modelBuilder.Entity<FileAnalysisResult>()
            .HasIndex(r => r.FileId);
    }
}
