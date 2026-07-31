using FluentAssertions;
using SciFiEditor.Core.Manuscript;

namespace SciFiEditor.Core.Tests.Manuscript;

public class WordCountServiceTests
{
    [Fact]
    public void Count_EmptyString_ReturnsZero()
    {
        var (words, chars) = WordCountService.Count(string.Empty);

        words.Should().Be(0);
        chars.Should().Be(0);
    }

    [Fact]
    public void Count_WhitespaceOnly_ReturnsZeroWords()
    {
        var (words, chars) = WordCountService.Count("   \n\t  ");

        words.Should().Be(0);
        chars.Should().Be(7);
    }

    [Fact]
    public void Count_MultipleWords_CountsWordsAndChars()
    {
        var text = "The quick brown fox";

        var (words, chars) = WordCountService.Count(text);

        words.Should().Be(4);
        chars.Should().Be(text.Length);
    }

    [Fact]
    public void Count_UnicodeText_CountsWordsCorrectly()
    {
        var text = "Швидка лисиця стрибає";

        var (words, chars) = WordCountService.Count(text);

        words.Should().Be(3);
        chars.Should().Be(text.Length);
    }
}
