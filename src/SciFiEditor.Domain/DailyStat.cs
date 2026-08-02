namespace SciFiEditor.Domain;

public sealed record DailyStat(DateOnly Date, int WordCountTotal, int WordsWritten);
