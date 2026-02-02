using StranglerSeamDemo.Api.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StranglerSeamDemo.Api.Models;

namespace StranglerSeamDemo.Api.Tests;

public class CasesApiTests
{
    [Fact]
    public async Task GetCases_ReturnsPagedResult_WithSearchAndPaging()
    {
        await using var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace AppDbContext registration with in-memory SQLite
                    var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    services.Remove(descriptor);

                    var conn = new SqliteConnection("Data Source=:memory:");
                    conn.Open();

                    services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(conn));

                    // Ensure DB created and seeded
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.EnsureCreated();

                    if (!db.Cases.Any())
                    {
                        db.Cases.Add(new CaseRecord { PatientName = "Ann A.", Procedure = "CT", Status = "New" });
                        db.Cases.Add(new CaseRecord { PatientName = "Ann B.", Procedure = "MRI", Status = "Done" });
                        db.Cases.Add(new CaseRecord { PatientName = "Bob C.", Procedure = "X-Ray", Status = "New" });
                        db.SaveChanges();
                    }
                });
            });

        var client = app.CreateClient();

        var resp = await client.GetAsync("/cases?search=Ann&page=1&pageSize=1");
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();

        Assert.Contains("\"total\":2", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"page\":1", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"pageSize\":1", json, StringComparison.OrdinalIgnoreCase);
    }
}
