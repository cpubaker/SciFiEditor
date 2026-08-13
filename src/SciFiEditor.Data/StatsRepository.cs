using System.Globalization;
using Dapper;
using SciFiEditor.Domain;

namespace SciFiEditor.Data;

public sealed class StatsRepository
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string SelectColumns = "date AS Date, word_count_total AS WordCountTotal, words_written AS WordsWritten";

    private readonly ProjectDatabase _database;

    public StatsRepository(ProjectDatabase database)
    {
        _database = database;
    }

    public void UpsertDailyStat(DateOnly date, int wordCountTotal, int wordsWritten)
    {
        const string sql = """
            INSERT INTO daily_stats (date, word_count_total, words_written)
            VALUES (@Date, @WordCountTotal, @WordsWritten)
            ON CONFLICT(date) DO UPDATE SET word_count_total = excluded.word_count_total, words_written = excluded.words_written
            """;
        _database.Connection.Execute(sql, new
        {
            Date = Format(date),
            WordCountTotal = wordCountTotal,
            WordsWritten = wordsWritten
        });
    }

    public DailyStat? GetByDate(DateOnly date)
    {
        var sql = $"SELECT {SelectColumns} FROM daily_stats WHERE date = @Date";
        var row = _database.Connection.QuerySingleOrDefault<StatRow>(sql, new { Date = Format(date) });
        return row is null ? null : MapRow(row);
    }

    public IReadOnlyList<DailyStat> GetRange(DateOnly from, DateOnly to)
    {
        var sql = $"SELECT {SelectColumns} FROM daily_stats WHERE date >= @From AND date <= @To ORDER BY date";
        return _database.Connection
            .Query<StatRow>(sql, new { From = Format(from), To = Format(to) })
            .Select(MapRow)
            .ToList();
    }

    public DailyStat? GetMostRecentBefore(DateOnly date)
    {
        var sql = $"SELECT {SelectColumns} FROM daily_stats WHERE date < @Date ORDER BY date DESC LIMIT 1";
        var row = _database.Connection.QuerySingleOrDefault<StatRow>(sql, new { Date = Format(date) });
        return row is null ? null : MapRow(row);
    }

    public ProjectGoals GetGoals()
    {
        const string sql = "SELECT daily_goal AS DailyGoal, session_goal AS SessionGoal FROM project_settings WHERE id = 1";
        var row = _database.Connection.QuerySingleOrDefault<GoalsRow>(sql);
        return row is null
            ? new ProjectGoals(null, null)
            : new ProjectGoals((int?)row.DailyGoal, (int?)row.SessionGoal);
    }

    public void SetGoals(int? dailyGoal, int? sessionGoal)
    {
        const string sql = """
            INSERT INTO project_settings (id, daily_goal, session_goal)
            VALUES (1, @DailyGoal, @SessionGoal)
            ON CONFLICT(id) DO UPDATE SET daily_goal = excluded.daily_goal, session_goal = excluded.session_goal
            """;
        _database.Connection.Execute(sql, new { DailyGoal = dailyGoal, SessionGoal = sessionGoal });
    }

    public Guid? GetLastSelectedNodeId()
    {
        const string sql = "SELECT last_selected_node_id FROM project_settings WHERE id = 1";
        var value = _database.Connection.QuerySingleOrDefault<string?>(sql);
        return value is null ? null : Guid.Parse(value);
    }

    public void SetLastSelectedNodeId(Guid? nodeId)
    {
        const string sql = """
            INSERT INTO project_settings (id, last_selected_node_id)
            VALUES (1, @NodeId)
            ON CONFLICT(id) DO UPDATE SET last_selected_node_id = excluded.last_selected_node_id
            """;
        _database.Connection.Execute(sql, new { NodeId = nodeId?.ToString() });
    }

    private static string Format(DateOnly date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

    private static DailyStat MapRow(StatRow row) => new(
        DateOnly.ParseExact(row.Date, DateFormat, CultureInfo.InvariantCulture),
        row.WordCountTotal,
        row.WordsWritten);

    private sealed class StatRow
    {
        public string Date { get; set; } = string.Empty;
        public int WordCountTotal { get; set; }
        public int WordsWritten { get; set; }
    }

    private sealed class GoalsRow
    {
        public long? DailyGoal { get; set; }
        public long? SessionGoal { get; set; }
    }
}
