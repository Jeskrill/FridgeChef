namespace FridgeChef.SharedKernel;

public static class LikeHelper
{
    private const string EscapeChar = "\\";

    public static string EscapeForLike(string input) =>
        input
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");

    public static string ContainsPattern(string input) =>
        $"%{EscapeForLike(input)}%";

    public static string EscapeCharacter => EscapeChar;
}
