namespace ServicosApp.Application.DTOs.Fiscal;

public static class FiscalProviderCodeNormalizer
{
    public static string Normalize(string providerCode)
    {
        if (string.IsNullOrWhiteSpace(providerCode))
            throw new ArgumentException("Provider code is required.", nameof(providerCode));

        return new string(providerCode
            .Trim()
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    public static string? NormalizeOrNull(string? providerCode)
        => string.IsNullOrWhiteSpace(providerCode) ? null : Normalize(providerCode);

    public static bool IsFake(string? providerCode)
        => string.Equals(
            NormalizeOrNull(providerCode),
            FiscalProviderCodes.Fake,
            StringComparison.OrdinalIgnoreCase);
}
