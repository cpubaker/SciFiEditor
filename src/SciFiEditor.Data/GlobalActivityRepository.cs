using System.Globalization;
using Dapper;
using SciFiEditor.Domain;

namespace SciFiEditor.Data;

public sealed class GlobalActivityRepository
{
    private const string DateFormat = "yyyy-MM-dd";

    private readonly GlobalActivityDatabase _database;

    public GlobalActivityRepository(GlobalActivityDatabase database)
    {
        _database = database;
    }

    public void UpsertProjectDay(string projectPath, DateOnly date, int wordsWritten)
    {
        const string sql = """
            INSERT INTO project_daily_activity (project_path, date, words_written)
            VALUES (@ProjectPath, @Date, @WordsWritten)
            ON CONFLICT(project_path, date) DO UPDATE SET words_written = excluded.words_written
            """;
        _database.Connection.Execute(sql, new
        {
            ProjectPath = projectPath,
            Date = Format(date),
            WordsWritten = wordsWritten
        });
    }

    public IReadOnlyList<DailyStat> GetRange(DateOnly from, DateOnly to)
    {
        const string sql = """
            SELECT date AS Date, SUM(words_written) AS WordsWritten
            FROM project_daily_activity
            WHERE date >= @From AND date <= @To
            GROUP BY date
            ORDER BY date
            """;
        return _database.Connection
            .Query<StatRow>(sql, new { From = Format(from), To = Format(to) })
            .Select(row => new DailyStat(
                DateOnly.ParseExact(row.Date, DateFormat, CultureInfo.InvariantCulture),
                0,
                row.WordsWritten))
            .ToList();
    }

    public int GetWordsWrittenOn(DateOnly date)
    {
        const string sql = """
            SELECT SUM(words_written) FROM project_daily_activity WHERE date = @Date
            """;
        return _database.Connection.QuerySingleOrDefault<int?>(sql, new { Date = Format(date) }) ?? 0;
    }

    public int GetTotalDaysWithActivity()
    {
        const string sql = """
            SELECT COUNT(*) FROM (
                SELECT date FROM project_daily_activity GROUP BY date HAVING SUM(words_written) > 0
            )
            """;
        return _database.Connection.QuerySingle<int>(sql);
    }

    private static string Format(DateOnly date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

    private sealed class StatRow
    {
        public string Date { get; set; } = string.Empty;
        public int WordsWritten { get; set; }
    }
}
