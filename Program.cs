using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var notes = new List<Note>();
var nextId = 1;

// 1. Хелсчек
app.MapGet("/health", () => Results.Ok(new { 
    status = "ok", 
    time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") 
}));

// 2. Версия (ОСТАВИЛ ТОЛЬКО ОДИН РАЗ)
app.MapGet("/version", (IConfiguration config) => 
    Results.Ok(new { 
        name = config["App:Name"], 
        version = config["App:Version"] 
    }));

// 3. Работа с заметками
app.MapGet("/api/notes", () => notes);

app.MapGet("/api/notes/{id:int}", (int id) => 
    notes.FirstOrDefault(n => n.Id == id) is Note note 
        ? Results.Ok(note) 
        : Results.NotFound(new { message = "Заметка не найдена" }));

app.MapPost("/api/notes", (NoteInput input) => {
    if (string.IsNullOrWhiteSpace(input.Title)) 
        return Results.BadRequest(new { error = "Заголовок не может быть пустым" });

    var newNote = new Note(nextId++, input.Title, input.Text, DateTime.Now);
    notes.Add(newNote);
    return Results.Created($"/api/notes/{newNote.Id}", newNote);
});

app.MapDelete("/api/notes/{id:int}", (int id) => {
    var note = notes.FirstOrDefault(n => n.Id == id);
    if (note == null) return Results.NotFound();
    notes.Remove(note);
    return Results.NoContent();
});

// 4. Погода
var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        )).ToArray();
    return forecast;
});

// 5. База данных
app.MapGet("/db/ping", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("Mssql");
    using var connection = new SqlConnection(connectionString);
    try
    {
        await connection.OpenAsync();
        return Results.Ok(new { status = "ok", message = "Соединение с БД установлено" });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, title: "Ошибка подключения к БД", statusCode: 500);
    }
});

app.Run();

// Модели данных
public record Note(int Id, string Title, string Text, DateTime CreatedAt);
public record NoteInput(string Title, string Text);
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}