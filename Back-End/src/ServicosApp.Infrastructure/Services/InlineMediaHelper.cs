namespace ServicosApp.Infrastructure.Services;

public static class InlineMediaHelper
{
    public static bool TryParseDataUrl(string? value, out InlineMediaPayload? payload)
    {
        payload = null;

        if (string.IsNullOrWhiteSpace(value) ||
            !value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var commaIndex = value.IndexOf(',');
        if (commaIndex <= 5)
            return false;

        var metadata = value[5..commaIndex];
        var body = value[(commaIndex + 1)..];

        if (!metadata.Contains(";base64", StringComparison.OrdinalIgnoreCase))
            return false;

        var contentType = metadata.Split(';', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        if (string.IsNullOrWhiteSpace(contentType))
            contentType = "application/octet-stream";

        try
        {
            payload = new InlineMediaPayload
            {
                ContentType = contentType,
                Bytes = Convert.FromBase64String(body)
            };

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string ResolveExtension(string fileName, string contentType)
    {
        var fileExtension = Path.GetExtension(fileName)?.Trim();
        if (!string.IsNullOrWhiteSpace(fileExtension))
            return NormalizeExtension(fileExtension);

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            _ => ".bin"
        };
    }

    public static string ResolveContentTypeFromExtension(string storageKey)
    {
        var extension = Path.GetExtension(storageKey)?.ToLowerInvariant();

        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }

    public static string SlugifySegment(string value, string fallback)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length == 0)
            return fallback;

        Span<char> buffer = stackalloc char[normalized.Length];
        var index = 0;

        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                buffer[index++] = character;
            }
            else if (index > 0 && buffer[index - 1] != '-')
            {
                buffer[index++] = '-';
            }
        }

        var result = new string(buffer[..index]).Trim('-');
        return result.Length == 0 ? fallback : result;
    }

    private static string NormalizeExtension(string extension)
    {
        if (!extension.StartsWith(".", StringComparison.Ordinal))
            return $".{extension.Trim()}";

        return extension.Trim();
    }
}

public sealed class InlineMediaPayload
{
    public string ContentType { get; init; } = "application/octet-stream";
    public byte[] Bytes { get; init; } = [];
}
