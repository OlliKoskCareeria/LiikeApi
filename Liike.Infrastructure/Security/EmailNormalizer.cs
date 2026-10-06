namespace Liike.Infrastructure.Security;

internal static class EmailNormalizer
{
    public static string Normalize(string email) => email.Trim().ToUpperInvariant();

    public static string Clean(string email) => email.Trim();
}
