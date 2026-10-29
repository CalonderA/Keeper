using System.Text.Json.Serialization;
using Keeper.Api.Data;
using Keeper.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Строка подключения 'Default' не настроена.");

builder.Services.AddDbContext<KeeperDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IKeeperService, KeeperService>();

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new OpenApiInfo
{
    Title = "Keeper API — Акт II. Хранитель",
    Version = "v1",
    Description = "Взаимодействие посетителей с Хранителем замка."
}));

var app = builder.Build();

await ApplyMigrationsAsync(app, connectionString);

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Keeper API v1");
    options.RoutePrefix = "swagger";
});

app.MapControllers();

app.Run();

static async Task ApplyMigrationsAsync(WebApplication app, string connectionString)
{
    if (!app.Configuration.GetValue("Database:ApplyMigrations", true))
    {
        return;
    }

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<KeeperDbContext>();

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await EnsureDatabaseAsync(connectionString);
            await db.Database.MigrateAsync();
            return;
        }
        catch (Exception ex) when (attempt < 10)
        {
            app.Logger.LogWarning(ex, "Не удалось применить миграции (попытка {Attempt}). Повтор через 3 секунды.", attempt);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

static async Task EnsureDatabaseAsync(string connectionString)
{
    var builder = new NpgsqlConnectionStringBuilder(connectionString);
    if (string.IsNullOrWhiteSpace(builder.Database) || builder.Database == "postgres")
    {
        return;
    }

    var database = builder.Database;
    builder.Database = "postgres";

    await using var connection = new NpgsqlConnection(builder.ConnectionString);
    await connection.OpenAsync();

    await using (var check = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection))
    {
        check.Parameters.AddWithValue("name", database);
        if (await check.ExecuteScalarAsync() is not null)
        {
            return;
        }
    }

    await using var create = new NpgsqlCommand($"CREATE DATABASE \"{database.Replace("\"", "\"\"")}\"", connection);
    await create.ExecuteNonQueryAsync();
}
