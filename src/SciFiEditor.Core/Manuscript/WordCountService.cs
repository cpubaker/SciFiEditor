namespace SciFiEditor.Core.Manuscript;

public static class WordCountService
{
    public static (int Words, int Chars) Count(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return (0, 0);
        }

        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return (words, text.Length);
    }
}
