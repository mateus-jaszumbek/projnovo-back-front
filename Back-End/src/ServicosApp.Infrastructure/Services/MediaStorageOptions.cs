namespace ServicosApp.Infrastructure.Services;

public sealed class MediaStorageOptions
{
    public string Provider { get; set; } = "Local";
    public string PublicPathPrefix { get; set; } = "/media";
    public string LocalRootPath { get; set; } = "data/media";
}
