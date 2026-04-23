namespace ServicosApp.Application.Interfaces;

public interface IRemoteImageFetchService
{
    Task<RemoteImageFetchResult> DownloadAsync(
        string url,
        long maxBytes,
        IReadOnlyCollection<string> allowedContentTypes,
        CancellationToken cancellationToken);
}

public sealed class RemoteImageFetchResult
{
    public string FileName { get; init; } = "imagem";
    public string ContentType { get; init; } = "application/octet-stream";
    public byte[] Bytes { get; init; } = [];
}
