using Microsoft.Extensions.Options;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.Infrastructure.Services;

public sealed class MediaStorageService : IMediaStorageService
{
    private readonly MediaStorageOptions _options;

    public MediaStorageService(IOptions<MediaStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<StoredMediaFile> SaveAsync(
        string storageKey,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        EnsureLocalStorageOnly();

        var normalizedKey = NormalizeStorageKey(storageKey);
        var absolutePath = GetLocalAbsolutePath(normalizedKey);
        var directory = Path.GetDirectoryName(absolutePath);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await using var fileStream = new FileStream(
            absolutePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true);

        await content.CopyToAsync(fileStream, cancellationToken);

        return new StoredMediaFile
        {
            StorageKey = normalizedKey,
            PublicUrl = BuildPublicUrl(normalizedKey)
        };
    }

    public Task DeleteAsync(string? publicUrl, CancellationToken cancellationToken)
    {
        EnsureLocalStorageOnly();

        if (!TryExtractStorageKey(publicUrl, out var storageKey))
            return Task.CompletedTask;

        var absolutePath = GetLocalAbsolutePath(storageKey);
        if (File.Exists(absolutePath))
            File.Delete(absolutePath);

        return Task.CompletedTask;
    }

    public Task<MediaFileContent?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        EnsureLocalStorageOnly();

        var normalizedKey = NormalizeStorageKey(storageKey);
        var absolutePath = GetLocalAbsolutePath(normalizedKey);
        if (!File.Exists(absolutePath))
            return Task.FromResult<MediaFileContent?>(null);

        var stream = new FileStream(
            absolutePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            useAsync: true);

        return Task.FromResult<MediaFileContent?>(new MediaFileContent
        {
            Content = stream,
            ContentType = InlineMediaHelper.ResolveContentTypeFromExtension(normalizedKey),
            Length = stream.Length
        });
    }

    private void EnsureLocalStorageOnly()
    {
        if (!string.IsNullOrWhiteSpace(_options.Provider) &&
            !string.Equals(_options.Provider, "Local", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Este projeto esta configurado apenas para armazenamento local de midia.");
        }
    }

    private string GetLocalAbsolutePath(string storageKey)
    {
        var root = _options.LocalRootPath;
        if (string.IsNullOrWhiteSpace(root))
            root = "data/media";

        if (!Path.IsPathRooted(root))
            root = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), root));

        var segments = storageKey.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return Path.Combine((new[] { root }).Concat(segments).ToArray());
    }

    private string BuildPublicUrl(string storageKey)
    {
        var prefix = (_options.PublicPathPrefix ?? "/media").TrimEnd('/');
        var escapedKey = string.Join(
            "/",
            storageKey
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));

        return $"{(prefix.StartsWith("/") ? prefix : $"/{prefix}")}/{escapedKey}";
    }

    private static string NormalizeStorageKey(string storageKey)
    {
        var normalized = storageKey.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException("Chave de armazenamento invalida.");

        foreach (var segment in normalized.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment is "." or "..")
                throw new InvalidOperationException("Chave de armazenamento invalida.");
        }

        return normalized;
    }

    private bool TryExtractStorageKey(string? publicUrl, out string storageKey)
    {
        storageKey = string.Empty;

        if (string.IsNullOrWhiteSpace(publicUrl) ||
            publicUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var path = publicUrl.Trim();
        if (Uri.TryCreate(path, UriKind.Absolute, out var absoluteUri))
            path = absoluteUri.AbsolutePath;

        var candidates = new[]
        {
            $"{(_options.PublicPathPrefix ?? "/media").TrimEnd('/')}/",
            $"/api/{(_options.PublicPathPrefix ?? "/media").Trim('/').TrimEnd('/')}/"
        };

        foreach (var candidate in candidates)
        {
            if (!path.StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
                continue;

            var rawKey = path[candidate.Length..];
            storageKey = NormalizeStorageKey(Uri.UnescapeDataString(rawKey));
            return true;
        }

        return false;
    }
}
