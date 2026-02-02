using Microsoft.Data.Sqlite;

namespace StranglerSeamDemo.LegacyWinForms.Api;

public sealed class LegacySqliteCasesGateway : ICasesGateway
{
    private readonly string _dbPath;

    public LegacySqliteCasesGateway(string sqlitePath)
    {
        _dbPath = sqlitePath;
        EnsureCreatedAndSeeded();
    }

    private string ConnString => $"Data Source={_dbPath}";

    private void EnsureCreatedAndSeeded()
    {
        using var conn = new SqliteConnection(ConnString);
        conn.Open();

        // Create table if missing
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Cases (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PatientName TEXT NOT NULL,
                Procedure TEXT NOT NULL,
                Status TEXT NOT NULL,
                LastUpdatedUtc TEXT NOT NULL
            );
            """;
            cmd.ExecuteNonQuery();
        }

        // If empty, seed ~30 rows
        using (var countCmd = conn.CreateCommand())
        {
            countCmd.CommandText = "SELECT COUNT(1) FROM Cases;";
            var count = Convert.ToInt32(countCmd.ExecuteScalar());
            if (count > 0) return;
        }

        var statuses = new[] { "New", "InProgress", "OnHold", "Done", "Cancelled" };
        var procedures = new[] { "CT", "MRI", "X-Ray", "Ultrasound", "EKG", "Biopsy" };
        var names = new[] { "Alex", "Sam", "Jordan", "Taylor", "Morgan", "Casey", "Riley", "Jamie", "Avery", "Quinn" };
        var rng = new Random(123);

        using var tx = conn.BeginTransaction();
        for (int i = 0; i < 30; i++)
        {
            using var insert = conn.CreateCommand();
            insert.Transaction = tx;
            insert.CommandText = """
            INSERT INTO Cases (PatientName, Procedure, Status, LastUpdatedUtc)
            VALUES ($patient, $procedure, $status, $ts);
            """;
            insert.Parameters.AddWithValue("$patient", $"{names[rng.Next(names.Length)]} {((char)('A' + rng.Next(26)))}.");
            insert.Parameters.AddWithValue("$procedure", procedures[rng.Next(procedures.Length)]);
            insert.Parameters.AddWithValue("$status", statuses[rng.Next(statuses.Length)]);
            insert.Parameters.AddWithValue("$ts", DateTime.UtcNow.AddMinutes(-rng.Next(0, 60 * 24 * 7)).ToString("o"));
            insert.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public async Task<PagedResult<CaseDto>> GetCasesAsync(string? search, int page, int pageSize)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(pageSize));

        var term = (search ?? "").Trim();

        using var conn = new SqliteConnection(ConnString);
        await conn.OpenAsync();

        // Total
        int total;
        using (var totalCmd = conn.CreateCommand())
        {
            totalCmd.CommandText = """
            SELECT COUNT(1)
            FROM Cases
            WHERE ($term = '')
               OR PatientName LIKE '%' || $term || '%'
               OR Procedure   LIKE '%' || $term || '%'
               OR Status      LIKE '%' || $term || '%';
            """;
            totalCmd.Parameters.AddWithValue("$term", term);
            total = Convert.ToInt32(await totalCmd.ExecuteScalarAsync());
        }

        // Page
        var items = new List<CaseDto>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
            SELECT Id, PatientName, Procedure, Status, LastUpdatedUtc
            FROM Cases
            WHERE ($term = '')
               OR PatientName LIKE '%' || $term || '%'
               OR Procedure   LIKE '%' || $term || '%'
               OR Status      LIKE '%' || $term || '%'
            ORDER BY LastUpdatedUtc DESC
            LIMIT $take OFFSET $skip;
            """;
            cmd.Parameters.AddWithValue("$term", term);
            cmd.Parameters.AddWithValue("$take", pageSize);
            cmd.Parameters.AddWithValue("$skip", (page - 1) * pageSize);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new CaseDto(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    DateTime.Parse(reader.GetString(4)).ToUniversalTime()
                ));
            }
        }

        return new PagedResult<CaseDto>(items, total, page, pageSize);
    }

    public async Task<CaseDto> UpdateStatusAsync(int id, string status)
    {
        status = (status ?? "").Trim();
        if (string.IsNullOrWhiteSpace(status))
            throw new InvalidOperationException("status is required");

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "New", "InProgress", "OnHold", "Done", "Cancelled" };

        if (!allowed.Contains(status))
            throw new InvalidOperationException($"status must be one of: {string.Join(", ", allowed)}");

        using var conn = new SqliteConnection(ConnString);
        await conn.OpenAsync();

        // Check exists
        using (var existsCmd = conn.CreateCommand())
        {
            existsCmd.CommandText = "SELECT COUNT(1) FROM Cases WHERE Id = $id;";
            existsCmd.Parameters.AddWithValue("$id", id);
            var exists = Convert.ToInt32(await existsCmd.ExecuteScalarAsync()) > 0;
            if (!exists) throw new KeyNotFoundException("Case not found");
        }

        var now = DateTime.UtcNow.ToString("o");
        using (var update = conn.CreateCommand())
        {
            update.CommandText = """
            UPDATE Cases SET Status = $status, LastUpdatedUtc = $ts WHERE Id = $id;
            """;
            update.Parameters.AddWithValue("$status", status);
            update.Parameters.AddWithValue("$ts", now);
            update.Parameters.AddWithValue("$id", id);
            await update.ExecuteNonQueryAsync();
        }

        // Return updated
        using var get = conn.CreateCommand();
        get.CommandText = "SELECT Id, PatientName, Procedure, Status, LastUpdatedUtc FROM Cases WHERE Id = $id;";
        get.Parameters.AddWithValue("$id", id);

        using var r = await get.ExecuteReaderAsync();
        await r.ReadAsync();

        return new CaseDto(
            r.GetInt32(0),
            r.GetString(1),
            r.GetString(2),
            r.GetString(3),
            DateTime.Parse(r.GetString(4)).ToUniversalTime()
        );
    }
}
