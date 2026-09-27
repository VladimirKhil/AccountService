namespace AccountService.Services;

internal static class UsernameFormatter
{
    private const string Prefix = "Ⓢ";

    public static string FormatPublic(string username)
    {
        var normalized = username.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Prefix;
        }

        return normalized.StartsWith(Prefix, StringComparison.Ordinal)
            ? normalized
            : $"{Prefix}{normalized}";
    }

    public static string NormalizeForStorage(string username)
    {
        var normalized = username.Trim();

        if (normalized.StartsWith(Prefix, StringComparison.Ordinal))
        {
            normalized = normalized[Prefix.Length..].TrimStart();
        }

        return normalized;
    }
}
