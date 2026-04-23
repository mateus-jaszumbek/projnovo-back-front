namespace ServicosApp.Application.Interfaces;

public interface ITacCacheBootstrapService
{
    Task EnsureCacheReadyAsync(CancellationToken cancellationToken);
}
