using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.Infrastructure.Services;

public sealed class TacCacheBootstrapService : ITacCacheBootstrapService
{
    private const string DefaultTacCacheRelativePath = "data/imei-tac-cache.json";
    private const string OsmocomExportUrl = "http://tacdb.osmocom.org/export/tacdb.json";

    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<ImeiLookupOptions> _optionsMonitor;
    private readonly ILogger<TacCacheBootstrapService> _logger;

    public TacCacheBootstrapService(
        HttpClient httpClient,
        IOptionsMonitor<ImeiLookupOptions> optionsMonitor,
        ILogger<TacCacheBootstrapService> logger)
    {
        _httpClient = httpClient;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task EnsureCacheReadyAsync(CancellationToken cancellationToken)
    {
        var cachePath = ResolveTacCachePath(_optionsMonitor.CurrentValue.TacCacheFilePath);
        if (File.Exists(cachePath))
            return;

        var directory = Path.GetDirectoryName(cachePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, OsmocomExportUrl);
            request.Headers.UserAgent.ParseAdd("ServicosApp/1.0");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(sourceStream, cancellationToken: cancellationToken);
            var compactMap = BuildCompactTacMap(document.RootElement);

            await using var targetStream = new FileStream(
                cachePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true);

            await JsonSerializer.SerializeAsync(targetStream, compactMap, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Cache TAC gerado com sucesso em {Path}. Registros: {Count}",
                cachePath,
                compactMap.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nao foi possivel preparar o cache TAC local em {Path}", cachePath);
        }
    }

    private static Dictionary<string, CompactTacEntry> BuildCompactTacMap(JsonElement root)
    {
        var result = new Dictionary<string, CompactTacEntry>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty("brands", out var brands) || brands.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var brandProperty in brands.EnumerateObject())
        {
            var brand = brandProperty.Name.Trim();
            if (!brandProperty.Value.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var modelWrapper in models.EnumerateArray())
            {
                if (modelWrapper.ValueKind != JsonValueKind.Object)
                    continue;

                foreach (var modelProperty in modelWrapper.EnumerateObject())
                {
                    var model = modelProperty.Name.Trim();
                    if (string.IsNullOrWhiteSpace(model))
                        continue;

                    var altName = ReadFirstAltName(modelProperty.Value);
                    var name = !string.IsNullOrWhiteSpace(altName)
                        ? altName
                        : $"{brand} {model}".Trim();

                    if (!modelProperty.Value.TryGetProperty("tacs", out var tacs) || tacs.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var tacValue in tacs.EnumerateArray())
                    {
                        if (tacValue.ValueKind != JsonValueKind.String)
                            continue;

                        var tac = tacValue.GetString()?.Trim();
                        if (string.IsNullOrWhiteSpace(tac))
                            continue;

                        result[tac] = new CompactTacEntry
                        {
                            Brand = brand,
                            Model = model,
                            Name = name
                        };
                    }
                }
            }
        }

        return result;
    }

    private static string ReadFirstAltName(JsonElement modelElement)
    {
        if (!modelElement.TryGetProperty("alt_names", out var altNames) || altNames.ValueKind != JsonValueKind.Array)
            return string.Empty;

        foreach (var value in altNames.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.String)
                continue;

            var text = value.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return string.Empty;
    }

    private static string ResolveTacCachePath(string? configuredPath)
    {
        var value = string.IsNullOrWhiteSpace(configuredPath)
            ? DefaultTacCacheRelativePath
            : configuredPath;

        return Path.IsPathRooted(value)
            ? value
            : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), value));
    }

    private sealed class CompactTacEntry
    {
        public string Brand { get; init; } = string.Empty;
        public string Model { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
    }
}
