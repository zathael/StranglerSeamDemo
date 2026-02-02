using StranglerSeamDemo.Api.Data;
using StranglerSeamDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SQLite file in solution root (shared local datastore for the spike)
builder.Services.AddDbContext<AppDbContext>(opt =>
{
var cs = builder.Configuration.GetConnectionString("CasesDb");
opt.UseSqlite(cs);
});

var app = builder.Build();

// Seed on startup (quick and simple for spike)
using (var scope = app.Services.CreateScope())
{
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
db.Database.EnsureCreated();

if (!db.Cases.Any())
{
var statuses = new[] { "New", "InProgress", "OnHold", "Done", "Cancelled" };
var procedures = new[] { "CT", "MRI", "X-Ray", "Ultrasound", "EKG", "Biopsy" };
var names = new[] { "Alex", "Sam", "Jordan", "Taylor", "Morgan", "Casey", "Riley", "Jamie", "Avery", "Quinn" };

var rng = new Random(123);

for (int i = 1; i <= 30; i++)
{
db.Cases.Add(new CaseRecord
{
PatientName = $"{names[rng.Next(names.Length)]} {((char)('A' + rng.Next(26)))}.",
Procedure = procedures[rng.Next(procedures.Length)],
Status = statuses[rng.Next(statuses.Length)],
LastUpdatedUtc = DateTime.UtcNow.AddMinutes(-rng.Next(0, 60 * 24 * 7))
});
}
db.SaveChanges();
}
}

if (app.Environment.IsDevelopment())
{
app.UseSwagger();
app.UseSwaggerUI();
}

app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// For WebApplicationFactory in tests
public partial class Program { }
