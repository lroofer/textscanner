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

// Подключение к базе данных с повторными попытками
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
            
            // Проверяем подключение
            dbContext.Database.OpenConnection();
            
            // Инициализируем базу данных
            var connection = dbContext.Database.GetDbConnection();
            
            // Разделяем создание таблиц на отдельные команды для лучшей обработки ошибок
            using (var command = connection.CreateCommand())
            {
                // 1. Проверяем существование таблицы миграций
                command.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory')";
                bool migrationsTableExists = (bool)command.ExecuteScalar();
                
                // 2. Создаем таблицу миграций, если её нет
                if (!migrationsTableExists)
                {
                    app.Logger.LogInformation("Создаем таблицу __EFMigrationsHistory...");
                    using (var createMigrationsCommand = connection.CreateCommand())
                    {
                        createMigrationsCommand.CommandText = @"
                            CREATE TABLE ""__EFMigrationsHistory"" (
                                ""MigrationId"" character varying(150) NOT NULL,
                                ""ProductVersion"" character varying(32) NOT NULL,
                                CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
                            );
                        ";
                        createMigrationsCommand.ExecuteNonQuery();
                        app.Logger.LogInformation("Таблица __EFMigrationsHistory создана успешно.");
                    }
                }
                
                // 3. Проверяем существование таблицы Files
                command.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Files')";
                bool filesTableExists = (bool)command.ExecuteScalar();
                
                // 4. Создаем таблицу Files, если её нет
                if (!filesTableExists)
                {
                    app.Logger.LogInformation("Создаем таблицу Files...");
                    using (var createFilesCommand = connection.CreateCommand())
                    {
                        createFilesCommand.CommandText = @"
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
                        createFilesCommand.ExecuteNonQuery();
                        app.Logger.LogInformation("Таблица Files создана успешно.");
                    }
                }
                
                // 5. Добавляем запись о миграции в таблицу __EFMigrationsHistory
                command.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '20230522000000_InitialCreate'";
                var count = Convert.ToInt32(command.ExecuteScalar());
                
                if (count == 0)
                {
                    app.Logger.LogInformation("Добавляем запись о миграции в __EFMigrationsHistory...");
                    using (var insertMigrationCommand = connection.CreateCommand())
                    {
                        insertMigrationCommand.CommandText = @"
                            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                            VALUES ('20230522000000_InitialCreate', '9.0.0-preview.2.24128.4');
                        ";
                        insertMigrationCommand.ExecuteNonQuery();
                        app.Logger.LogInformation("Запись о миграции добавлена успешно.");
                    }
                }
            }
            
            dbContext.Database.CloseConnection();
            databaseInitialized = true;
            app.Logger.LogInformation("База данных успешно инициализирована.");
        }
    }
    catch (Exception ex)
    {
        retryCount++;
        
        if (retryCount < maxRetries)
        {
            app.Logger.LogWarning(ex, $"Ошибка подключения к базе данных. Повторная попытка через 5 секунд... ({retryCount}/{maxRetries})");
            // Ждем 5 секунд перед повторной попыткой
            Thread.Sleep(5000);
        }
        else
        {
            app.Logger.LogError(ex, "Не удалось инициализировать базу данных после нескольких попыток.");
            throw;
        }
    }
}

// Добавляем тестовый эндпоинт для проверки доступности таблицы Files
app.MapGet("/api/debug/check-files-table", async (FileDbContext dbContext) =>
{
    try
    {
        // Проверяем, что таблица Files существует и доступна
        var connection = dbContext.Database.GetDbConnection();
        connection.Open();
        
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Files')";
            var exists = (bool)command.ExecuteScalar();
            
            if (exists)
            {
                // Проверяем, можем ли мы выполнить запрос
                var count = await dbContext.Files.CountAsync();
                connection.Close();
                return Results.Ok(new { Status = "OK", TableExists = true, RecordsCount = count });
            }
            else
            {
                connection.Close();
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
