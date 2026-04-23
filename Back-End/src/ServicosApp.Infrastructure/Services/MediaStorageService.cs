using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.Infrastructure.Services;

public sealed class MediaStorageService : IMediaStorageService
{
    private readonly MediaStorageOptions _options;
    private readonly Lazy<IAmazonS3?> _s3Client;

    public MediaStorageService(IOptions<MediaStorageOptions> options)
    {
        _options = options.Value;
        _s3Client = new Lazy<IAmazonS3?>(CreateS3Client);
    }

    public async Task<StoredMediaFile> SaveAsync(
        string storageKey,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        var normalizedKey = NormalizeStorageKey(storageKey);
        var normalizedContentType = string.IsNullOrWhiteSpace(contentType)
            ? InlineMediaHelper.ResolveContentTypeFromExtension(fileName)
            : contentType.Trim();

        if (UseS3())
        {
            var client = _s3Client.Value ?? throw new InvalidOperationException("Cliente S3 nao configurado.");
            var request = new PutObjectRequest
            {
                BucketName = _options.S3BucketName,
                Key = BuildS3ObjectKey(normalizedKey),
                InputStream = content,
                ContentType = normalizedContentType,
                AutoCloseStream = false
            };

            try
            {
                await client.PutObjectAsync(request, cancellationToken);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.Unauthorized || string.Equals(ex.ErrorCode, "AccessDenied", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("O IAM configurado para o S3 nao tem permissao para gravar arquivos no bucket.", ex);
            }
        }
        else
        {
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
        }

        return new StoredMediaFile
        {
            StorageKey = normalizedKey,
            PublicUrl = BuildPublicUrl(normalizedKey)
        };
    }

    public async Task DeleteAsync(string? publicUrl, CancellationToken cancellationToken)
    {
        if (!TryExtractStorageKey(publicUrl, out var storageKey))
            return;

        if (UseS3())
        {
            var client = _s3Client.Value;
            if (client is null)
                return;

            await client.DeleteObjectAsync(
                new DeleteObjectRequest
                {
                    BucketName = _options.S3BucketName,
                    Key = BuildS3ObjectKey(storageKey)
                },
                cancellationToken);

            return;
        }

        var absolutePath = GetLocalAbsolutePath(storageKey);
        if (File.Exists(absolutePath))
            File.Delete(absolutePath);
    }

    public async Task<MediaFileContent?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var normalizedKey = NormalizeStorageKey(storageKey);

        if (UseS3())
        {
            var client = _s3Client.Value;
            if (client is null)
                return null;

            try
            {
                using var response = await client.GetObjectAsync(
                    _options.S3BucketName,
                    BuildS3ObjectKey(normalizedKey),
                    cancellationToken);

                var memory = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memory, cancellationToken);
                memory.Position = 0;

                return new MediaFileContent
                {
                    Content = memory,
                    ContentType = string.IsNullOrWhiteSpace(response.Headers.ContentType)
                        ? InlineMediaHelper.ResolveContentTypeFromExtension(normalizedKey)
                        : response.Headers.ContentType,
                    Length = memory.Length
                };
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        var absolutePath = GetLocalAbsolutePath(normalizedKey);
        if (!File.Exists(absolutePath))
            return null;

        var stream = new FileStream(
            absolutePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            useAsync: true);

        return new MediaFileContent
        {
            Content = stream,
            ContentType = InlineMediaHelper.ResolveContentTypeFromExtension(normalizedKey),
            Length = stream.Length
        };
    }

    private bool UseS3()
        => string.Equals(_options.Provider, "S3", StringComparison.OrdinalIgnoreCase);

    private IAmazonS3? CreateS3Client()
    {
        if (!UseS3())
            return null;

        if (string.IsNullOrWhiteSpace(_options.S3BucketName))
            throw new InvalidOperationException("MediaStorage:S3BucketName deve ser configurado quando Provider=S3.");

        var config = new AmazonS3Config();

        if (!string.IsNullOrWhiteSpace(_options.S3Region))
            config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_options.S3Region);

        if (!string.IsNullOrWhiteSpace(_options.S3ServiceUrl))
        {
            config.ServiceURL = _options.S3ServiceUrl;
            config.ForcePathStyle = _options.S3ForcePathStyle;
        }

        if (!string.IsNullOrWhiteSpace(_options.S3AccessKey) &&
            !string.IsNullOrWhiteSpace(_options.S3SecretKey))
        {
            return new AmazonS3Client(
                new BasicAWSCredentials(_options.S3AccessKey, _options.S3SecretKey),
                config);
        }

        return new AmazonS3Client(config);
    }

    private string BuildS3ObjectKey(string storageKey)
    {
        var prefix = (_options.S3Prefix ?? string.Empty).Trim().Trim('/');
        return string.IsNullOrWhiteSpace(prefix) ? storageKey : $"{prefix}/{storageKey}";
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
