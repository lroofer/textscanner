using FileStoringService.Data;
using FileStoringService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<FileDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("FileStoringDb"));
});

builder.Services.AddScoped<FileStorageService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<FileDbContext>();
        var connection = dbContext.Database.GetDbConnection();
        
        app.Logger.LogInformation("Initializing database...");
        
        try
        {
            connection.Open();
            
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory')";
                var migrationsTableExists = (bool)command.ExecuteScalar();
                
                if (!migrationsTableExists)
                {
                    app.Logger.LogInformation("Creating __EFMigrationsHistory table...");
                    using (var createMigrationTableCommand = connection.CreateCommand())
                    {
                        createMigrationTableCommand.CommandText = @"
                            CREATE TABLE ""__EFMigrationsHistory"" (
                                ""MigrationId"" character varying(150) NOT NULL,
                                ""ProductVersion"" character varying(32) NOT NULL,
                                CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
                            );
                        ";
                        createMigrationTableCommand.ExecuteNonQuery();
                    }
                }
            }
            
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Files')";
                var filesTableExists = (bool)command.ExecuteScalar();
                
                if (!filesTableExists)
                {
                    app.Logger.LogInformation("Creating Files table...");
                    using (var createFilesTableCommand = connection.CreateCommand())
                    {
                        createFilesTableCommand.CommandText = @"
                            CREATE TABLE ""Files"" (
                                ""Id"" uuid NOT NULL,
                                ""FileName"" text NOT NULL,
                                ""Hash"" text NOT NULL,
                                ""Location"" text NOT NULL,
                                ""CreatedAt"" timestamp with time zone NOT NULL,
                                CONSTRAINT ""PK_Files"" PRIMARY KEY (""Id"")
                            );
                            
                            CREATE UNIQUE INDEX ""IX_Files_Hash"" ON ""Files"" (""Hash"");
                        ";
                        createFilesTableCommand.ExecuteNonQuery();
                    }
                    
                    // Записываем информацию о "миграции" в таблицу __EFMigrationsHistory
                    using (var insertMigrationCommand = connection.CreateCommand())
                    {
                        insertMigrationCommand.CommandText = @"
                            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                            VALUES ('20230522000000_InitialCreate', '9.0.0-preview.2.24128.4')
                            ON CONFLICT DO NOTHING;
                        ";
                        insertMigrationCommand.ExecuteNonQuery();
                    }
                    
                    app.Logger.LogInformation("Database initialized successfully.");
                }
                else
                {
                    app.Logger.LogInformation("Files table already exists.");
                }
            }
        }
        finally
        {
            if (connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
            }
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
