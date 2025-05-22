using FileStoringService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileStoringService.Data;

public class FileDbContext : DbContext
{
    public FileDbContext(DbContextOptions<FileDbContext> options) : base(options)
    {
    }

    public DbSet<FileEntity> Files { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileEntity>()
            .HasIndex(f => f.Hash)
            .IsUnique();
    }
}
