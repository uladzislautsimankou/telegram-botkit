using Telegram.BotKit.Helpers;

namespace Telegram.BotKit.Tests.Helpers;

public class CommandSearcherTests
{
    private readonly CommandSearcher _searcher = new CommandSearcher([
        "/ping",
        "/bday_check",
        "/weather_minsk"
    ]);

    [Theory]
    [InlineData("png", "/ping")]           // пропуск буквы
    [InlineData("pnig", "/ping")]          // перестановка букв
    [InlineData("pingg", "/ping")]         // лишняя буква
    [InlineData("pibg", "/ping")]          // опечатка (n -> b)
    [InlineData("/ping", "/ping")]         // точное совпадение
    public void Should_Find_Ping_Variations(string input, string expected)
    {
        var result = _searcher.FindSimilar(input);
        Assert.Contains(expected, result);
    }

    [Theory]
    [InlineData("check_bday", "/bday_check")]   // перестановка слов
    [InlineData("chek_bday", "/bday_check")]    // перестановка слов + опечатка
    [InlineData("checkbday", "/bday_check")]    // забыли подчеркивание
    [InlineData("bdaycheck", "/bday_check")]    // слитно
    [InlineData("bday_chek", "/bday_check")]    // опечатка
    public void Should_Find_BdayCheck_Variations(string input, string expected)
    {
        var result = _searcher.FindSimilar(input);
        Assert.Contains(expected, result);
    }

    [Theory]
    [InlineData("wether", "/weather_minsk")]    // не дописали
    public void Should_Find_Long_Command_Variations(string input, string expected)
    {
        var result = _searcher.FindSimilar(input);
        Assert.Contains(expected, result);
    }

    [Fact]
    public void Should_Return_Empty_For_Total_Nonsense()
    {
        var result = _searcher.FindSimilar("some_random_command");
        Assert.Empty(result);
    }
}
