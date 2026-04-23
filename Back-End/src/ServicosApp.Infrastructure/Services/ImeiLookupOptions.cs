namespace ServicosApp.Infrastructure.Services;

public sealed class ImeiLookupOptions
{
    public bool EnableExternalLookup { get; set; }
    public string? UrlTemplate { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiKeyHeader { get; set; }
    public string? TacCacheFilePath { get; set; }
    public List<ImeiLookupProviderOptions> Providers { get; set; } = new();
}

public sealed class ImeiLookupProviderOptions
{
    public string Name { get; set; } = "provider";
    public string? UrlTemplate { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiKeyHeader { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
