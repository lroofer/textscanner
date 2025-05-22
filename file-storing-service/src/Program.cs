using FileStoringService.Data;
using FileStoringService.Services;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

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

bool databaseInitialized = false;
const int maxRetries = 10;
int retryCount = 0;

while (!databaseInitialized && retryCount < maxRetries)
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var dbContext = services.GetRequiredService<FileDbContext>();
            
            app.Logger.LogInformation($"Попытка подключения к базе данных #{retryCount + 1}...");
            
            dbContext.Database.OpenConnection();
            dbContext.Database.CloseConnection();
            
            var connection = dbContext.Database.GetDbConnection();
            connection.Open();
            
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Files')";
                var filesTableExists = (bool)command.ExecuteScalar();
                
                if (!filesTableExists)
                {
                    app.Logger.LogWarning("Таблица Files не найдена. Создаем...");
                    
                    using (var createTableCommand = connection.CreateCommand())
                    {
                        createTableCommand.CommandText = @"
                            CREATE TABLE IF NOT EXISTS ""Files"" (
                                ""Id"" uuid NOT NULL,
                                ""FileName"" text NOT NULL,
                                ""Hash"" text NOT NULL,
                                ""Location"" text NOT NULL,
                                ""CreatedAt"" timestamp with time zone NOT NULL,
                                CONSTRAINT ""PK_Files"" PRIMARY KEY (""Id"")
                            );
                            
                            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Files_Hash"" ON ""Files"" (""Hash"");
                            
                            -- Добавляем запись в таблицу миграций, если она существует
                            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                            SELECT '20230522000000_InitialCreate', '9.0.0-preview.2.24128.4'
                            WHERE EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory')
                            ON CONFLICT DO NOTHING;
                        ";
                        createTableCommand.ExecuteNonQuery();
                        app.Logger.LogInformation("Таблица Files успешно создана.");
                    }
                }
                else
                {
                    app.Logger.LogInformation("Таблица Files уже существует.");
                }
            }
            
            connection.Close();
            databaseInitialized = true;
            app.Logger.LogInformation("Подключение к базе данных успешно установлено.");
        }
    }
    catch (Exception ex)
    {
        retryCount++;
        
        if (retryCount < maxRetries)
        {
            app.Logger.LogWarning(ex, $"Ошибка подключения к базе данных. Повторная попытка через 5 секунд... ({retryCount}/{maxRetries})");
            Thread.Sleep(5000);
        }
        else
        {
            app.Logger.LogError(ex, "Не удалось подключиться к базе данных после нескольких попыток.");
            throw;
        }
    }
}

app.MapGet("/api/debug/check-files-table", async (FileDbContext dbContext) =>
{
    try
    {
        var connection = dbContext.Database.GetDbConnection();
        connection.Open();
        
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Files')";
            var exists = (bool)command.ExecuteScalar();
            connection.Close();
            
            if (exists)
            {
                var count = await dbContext.Files.CountAsync();
                return Results.Ok(new { Status = "OK", TableExists = true, RecordsCount = count });
            }
            else
            {
                return Results.Ok(new { Status = "Error", TableExists = false, Message = "Таблица Files не существует" });
            }
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.ToString());
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
