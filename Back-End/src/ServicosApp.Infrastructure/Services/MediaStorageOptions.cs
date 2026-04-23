namespace ServicosApp.Infrastructure.Services;

public sealed class MediaStorageOptions
{
    public string Provider { get; set; } = "Local";
    public string PublicPathPrefix { get; set; } = "/media";
    public string LocalRootPath { get; set; } = "data/media";
    public string? S3BucketName { get; set; }
    public string? S3Region { get; set; }
    public string? S3AccessKey { get; set; }
    public string? S3SecretKey { get; set; }
    public string? S3ServiceUrl { get; set; }
    public bool S3ForcePathStyle { get; set; }
    public string? S3Prefix { get; set; }
}
