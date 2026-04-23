using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.Infrastructure.Services;

public sealed class ImeiLookupService : IImeiLookupService
{
    private const string DefaultTacCacheRelativePath = "data/imei-tac-cache.json";
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<ImeiLookupOptions> _optionsMonitor;
    private readonly ILogger<ImeiLookupService> _logger;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    private string? _cachedTacPath;
    private DateTime _cachedTacLastWriteUtc;
    private Dictionary<string, TacCacheEntry>? _tacCache;

    public ImeiLookupService(
        HttpClient httpClient,
        IOptionsMonitor<ImeiLookupOptions> optionsMonitor,
        ILogger<ImeiLookupService> logger)
    {
        _httpClient = httpClient;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task<ImeiLookupDto> ConsultarAsync(string imei, CancellationToken cancellationToken)
    {
        var normalized = OnlyDigits(imei);
        var result = new ImeiLookupDto
        {
            Imei = normalized,
            Tac = normalized.Length >= 8 ? normalized[..8] : string.Empty,
            Valido = IsValidImei(normalized)
        };

        if (!result.Valido)
        {
            result.Mensagem = "IMEI invalido. Informe 15 digitos validos.";
            return result;
        }

        var cacheEntry = await ConsultarTacCacheAsync(result.Tac, cancellationToken);
        if (cacheEntry is not null)
        {
            result.Marca = cacheEntry.Brand;
            result.Modelo = cacheEntry.Model;
            result.NomeComercial = cacheEntry.Name;
            result.Encontrado = true;
            result.Fonte = "tac-cache";
            result.Mensagem = "Dados encontrados pelo cache TAC local.";
            return result;
        }

        var options = _optionsMonitor.CurrentValue;
        if (!options.EnableExternalLookup)
        {
            result.Fonte = "tac-cache";
            result.Mensagem = "IMEI valido, mas o TAC nao foi encontrado no cache local.";
            return result;
        }

        string? providerMessage = null;

        foreach (var provider in GetProviders())
        {
            if (string.IsNullOrWhiteSpace(provider.UrlTemplate))
                continue;

            var providerResult = await ConsultarProviderAsync(provider, result, cancellationToken);
            if (providerResult.Encontrado)
                return providerResult;

            providerMessage = providerResult.Mensagem;
        }
        result.Fonte = result.Fonte ?? "imei";
        result.Mensagem = providerMessage ?? "IMEI valido, mas nenhum provedor retornou dados.";
        return result;
    }

    private async Task<ImeiLookupDto> ConsultarProviderAsync(
        ImeiLookupProviderOptions provider,
        ImeiLookupDto seed,
        CancellationToken cancellationToken)
    {
        var result = Clone(seed);
        var url = BuildProviderUrl(provider, seed.Imei, seed.Tac);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("ServicosApp/1.0");

            foreach (var header in provider.Headers)
            {
                if (!string.IsNullOrWhiteSpace(header.Value))
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            var apiKey = provider.ApiKey?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(provider.ApiKeyHeader) && !string.IsNullOrWhiteSpace(apiKey))
                request.Headers.TryAddWithoutValidation(provider.ApiKeyHeader, apiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            result.Fonte = provider.Name;

            if (!response.IsSuccessStatusCode)
            {
                result.Mensagem = MapProviderFailureMessage(response.StatusCode);
                return result;
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!TryPopulateFromJson(payload, result, out var providerMessage))
                result.Mensagem = providerMessage ?? "Consulta concluida, mas o provedor nao retornou marca ou modelo.";
            else
                result.Mensagem = providerMessage ?? "Dados encontrados pelo IMEI.";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao consultar provedor de IMEI {Provider}", provider.Name);
            result.Fonte = provider.Name;
            result.Mensagem = "Nao foi possivel consultar o provedor de IMEI agora.";
            return result;
        }
    }

    private IEnumerable<ImeiLookupProviderOptions> GetProviders()
    {
        var options = _optionsMonitor.CurrentValue;

        if (!options.EnableExternalLookup)
            return [];

        if (options.Providers.Count > 0)
            return options.Providers;

        if (string.IsNullOrWhiteSpace(options.UrlTemplate))
            return [];

        return
        [
            new ImeiLookupProviderOptions
            {
                Name = "imeicheck",
                UrlTemplate = options.UrlTemplate,
                ApiKey = options.ApiKey,
                ApiKeyHeader = options.ApiKeyHeader
            }
        ];
    }

    private async Task<TacCacheEntry?> ConsultarTacCacheAsync(string tac, CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        var cachePath = ResolveTacCachePath(options.TacCacheFilePath);
        if (string.IsNullOrWhiteSpace(cachePath) || !File.Exists(cachePath))
            return null;

        var lastWriteUtc = File.GetLastWriteTimeUtc(cachePath);
        if (_tacCache is not null &&
            string.Equals(_cachedTacPath, cachePath, StringComparison.OrdinalIgnoreCase) &&
            _cachedTacLastWriteUtc == lastWriteUtc)
        {
            return _tacCache.GetValueOrDefault(tac);
        }

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_tacCache is not null &&
                string.Equals(_cachedTacPath, cachePath, StringComparison.OrdinalIgnoreCase) &&
                _cachedTacLastWriteUtc == lastWriteUtc)
            {
                return _tacCache.GetValueOrDefault(tac);
            }

            await using var stream = File.OpenRead(cachePath);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            _tacCache = ParseTacCache(document.RootElement);
            _cachedTacPath = cachePath;
            _cachedTacLastWriteUtc = lastWriteUtc;

            return _tacCache.GetValueOrDefault(tac);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nao foi possivel carregar o cache TAC de {Path}", cachePath);
            return null;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private static Dictionary<string, TacCacheEntry> ParseTacCache(JsonElement root)
    {
        var map = new Dictionary<string, TacCacheEntry>(StringComparer.OrdinalIgnoreCase);

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.Object)
                    continue;

                var entry = ParseTacEntry(property.Value);
                if (entry is not null)
                    map[property.Name] = entry;
            }

            return map;
        }

        if (root.ValueKind != JsonValueKind.Array)
            return map;

        foreach (var item in root.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var tac = ReadString(item, "tac", "TAC");
            if (string.IsNullOrWhiteSpace(tac))
                continue;

            var entry = ParseTacEntry(item);
            if (entry is not null)
                map[tac] = entry;
        }

        return map;
    }

    private static TacCacheEntry? ParseTacEntry(JsonElement element)
    {
        var brand = ReadString(element, "brand", "manufacturer", "marca");
        var model = ReadString(element, "model", "modelo", "model_name");
        var name = ReadString(element, "name", "marketingName", "nome", "device");

        if (string.IsNullOrWhiteSpace(brand) &&
            string.IsNullOrWhiteSpace(model) &&
            string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return new TacCacheEntry(brand, model, name);
    }

    private static bool TryPopulateFromJson(
        string payload,
        ImeiLookupDto result,
        out string? providerMessage)
    {
        providerMessage = null;
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var providerStatus = ReadString(root, "status");
        if (!IsProviderSuccessStatus(providerStatus))
        {
            providerMessage = ExtractProviderMessage(root);
            return false;
        }

        var source = TryGetObject(root, "object", out var providerObject) ? providerObject : root;

        result.Marca = FirstNonEmpty(
            ReadString(source, "brand", "marca", "manufacturer", "device_brand"),
            ReadString(root, "brand", "marca", "manufacturer", "device_brand"));
        result.Modelo = FirstNonEmpty(
            ReadString(source, "model", "modelo", "device_model"),
            ReadString(root, "model", "modelo", "device_model"));
        result.NomeComercial = FirstNonEmpty(
            ReadString(source, "description", "name", "nome", "modelBrandName", "device", "marketingName"),
            ReadString(root, "description", "name", "nome", "modelBrandName", "device", "marketingName"),
            HtmlToText(ReadString(root, "result")));
        result.Cor = FirstNonEmpty(
            ReadString(source, "color", "colour", "cor"),
            ReadString(root, "color", "colour", "cor"));
        result.Capacidade = FirstNonEmpty(
            ReadString(source, "capacity", "capacidade"),
            ReadString(root, "capacity", "capacidade"));

        if (string.IsNullOrWhiteSpace(result.Marca))
            result.Marca = InferBrand(result.NomeComercial, result.Modelo);

        result.Encontrado = !string.IsNullOrWhiteSpace(result.Marca) ||
            !string.IsNullOrWhiteSpace(result.Modelo) ||
            !string.IsNullOrWhiteSpace(result.NomeComercial);

        providerMessage = ExtractProviderMessage(root);

        return result.Encontrado;
    }

    private static string BuildProviderUrl(ImeiLookupProviderOptions provider, string imei, string tac)
    {
        var apiKey = provider.ApiKey?.Trim() ?? string.Empty;

        return (provider.UrlTemplate ?? string.Empty)
            .Replace("{imei}", Uri.EscapeDataString(imei), StringComparison.OrdinalIgnoreCase)
            .Replace("{tac}", Uri.EscapeDataString(tac), StringComparison.OrdinalIgnoreCase)
            .Replace("{apiKey}", Uri.EscapeDataString(apiKey), StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveTacCachePath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            configuredPath = DefaultTacCacheRelativePath;

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredPath));
    }

    private static string MapProviderFailureMessage(System.Net.HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            System.Net.HttpStatusCode.Forbidden =>
                "O provedor externo recusou a consulta. Verifique a API key e o IP liberado na ImeiCheck.",
            System.Net.HttpStatusCode.TooManyRequests =>
                "O provedor de IMEI atingiu o limite de consultas agora.",
            _ => "Nao foi possivel consultar o provedor de IMEI agora."
        };
    }

    private static ImeiLookupDto Clone(ImeiLookupDto value)
        => new()
        {
            Imei = value.Imei,
            Tac = value.Tac,
            Valido = value.Valido,
            Marca = value.Marca,
            Modelo = value.Modelo,
            NomeComercial = value.NomeComercial,
            Cor = value.Cor,
            Capacidade = value.Capacidade,
            Encontrado = value.Encontrado,
            Fonte = value.Fonte,
            Mensagem = value.Mensagem
        };

    private static string ReadString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString()?.Trim() ?? string.Empty;
        }

        return string.Empty;
    }

    private static bool TryGetObject(JsonElement element, string name, out JsonElement value)
    {
        if (element.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.Object)
            return true;

        value = default;
        return false;
    }

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static bool IsProviderSuccessStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return true;

        return status.StartsWith("succ", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractProviderMessage(JsonElement root)
    {
        var directMessage = FirstNonEmpty(
            ReadString(root, "message", "error"),
            HtmlToText(ReadString(root, "result")));

        return string.IsNullOrWhiteSpace(directMessage)
            ? "Consulta concluida, mas o provedor nao retornou dados detalhados."
            : directMessage;
    }

    private static string HtmlToText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var withoutTags = Regex.Replace(html, "<br\\s*/?>", "\n", RegexOptions.IgnoreCase);
        withoutTags = Regex.Replace(withoutTags, "<[^>]+>", string.Empty, RegexOptions.IgnoreCase);
        return WebUtility.HtmlDecode(withoutTags).Trim();
    }

    private static string InferBrand(string? nomeComercial, string? modelo)
    {
        var sample = $"{nomeComercial} {modelo}".Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(sample))
            return string.Empty;

        if (sample.Contains("iphone") || sample.Contains("ipad") || sample.Contains("macbook"))
            return "Apple";
        if (sample.Contains("galaxy") || sample.Contains("samsung"))
            return "Samsung";
        if (sample.Contains("motorola") || sample.Contains("moto "))
            return "Motorola";
        if (sample.Contains("xiaomi") || sample.Contains("redmi") || sample.Contains("poco"))
            return "Xiaomi";
        if (sample.Contains("huawei"))
            return "Huawei";
        if (sample.Contains("realme"))
            return "Realme";
        if (sample.Contains("asus") || sample.Contains("rog phone") || sample.Contains("zenfone"))
            return "Asus";
        if (sample.Contains("nokia"))
            return "Nokia";
        if (sample.Contains("lg"))
            return "LG";
        if (sample.Contains("sony") || sample.Contains("xperia"))
            return "Sony";

        return string.Empty;
    }

    private static string OnlyDigits(string value)
        => new(value.Where(char.IsDigit).ToArray());

    private static bool IsValidImei(string imei)
    {
        if (imei.Length != 15 || !imei.All(char.IsDigit))
            return false;

        var sum = 0;
        for (var i = 0; i < imei.Length; i++)
        {
            var digit = imei[i] - '0';
            if (i % 2 == 1)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }

            sum += digit;
        }

        return sum % 10 == 0;
    }

    private sealed record TacCacheEntry(string Brand, string Model, string Name);
}
