namespace ServicosApp.Application.Interfaces;

public interface IMediaStorageService
{
    Task<StoredMediaFile> SaveAsync(
        string storageKey,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken);

    Task DeleteAsync(string? publicUrl, CancellationToken cancellationToken);

    Task<MediaFileContent?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
}

public sealed class StoredMediaFile
{
    public string StorageKey { get; init; } = string.Empty;
    public string PublicUrl { get; init; } = string.Empty;
}

public sealed class MediaFileContent : IAsyncDisposable
{
    public Stream Content { get; init; } = Stream.Null;
    public string ContentType { get; init; } = "application/octet-stream";
    public long? Length { get; init; }

    public ValueTask DisposeAsync() => Content.DisposeAsync();
}
