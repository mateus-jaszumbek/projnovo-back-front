using System.Net;
using System.Net.Sockets;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.Infrastructure.Services;

public sealed class RemoteImageFetchService : IRemoteImageFetchService
{
    private readonly HttpClient _httpClient;

    public RemoteImageFetchService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<RemoteImageFetchResult> DownloadAsync(
        string url,
        long maxBytes,
        IReadOnlyCollection<string> allowedContentTypes,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Informe uma URL HTTP ou HTTPS valida.");
        }

        await ValidateHostAsync(uri, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd("ServicosApp/1.0");

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Nao foi possivel baixar a imagem pela URL informada.");

        var contentType = response.Content.Headers.ContentType?.MediaType?.Trim() ?? string.Empty;
        if (!allowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A URL precisa apontar para uma imagem em formato permitido.");
        }

        if (response.Content.Headers.ContentLength.HasValue &&
            response.Content.Headers.ContentLength.Value > maxBytes)
            throw new InvalidOperationException($"A imagem da URL deve ter no maximo {Math.Round(maxBytes / 1024d / 1024d, 1)} MB.");

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var memory = new MemoryStream();
        var buffer = new byte[81920];
        long totalRead = 0;

        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0)
                break;

            totalRead += read;
            if (totalRead > maxBytes)
                throw new InvalidOperationException($"A imagem da URL deve ter no maximo {Math.Round(maxBytes / 1024d / 1024d, 1)} MB.");

            await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        var fileName = Path.GetFileName(uri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(fileName) || !Path.HasExtension(fileName))
            fileName = $"imagem{InlineMediaHelper.ResolveExtension(string.Empty, contentType)}";

        return new RemoteImageFetchResult
        {
            FileName = fileName,
            ContentType = contentType,
            Bytes = memory.ToArray()
        };
    }

    private static async Task ValidateHostAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("URL de localhost nao e permitida.");

        IPAddress[] addresses;
        if (IPAddress.TryParse(uri.Host, out var directAddress))
        {
            addresses = [directAddress];
        }
        else
        {
            addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
        }

        if (addresses.Length == 0)
            throw new InvalidOperationException("Nao foi possivel resolver a URL informada.");

        foreach (var address in addresses)
        {
            if (IsRestricted(address))
                throw new InvalidOperationException("A URL informada aponta para um endereco nao permitido.");
        }
    }

    private static bool IsRestricted(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
            return true;

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
                return true;

            var bytes = address.GetAddressBytes();
            if ((bytes[0] & 0xFE) == 0xFC)
                return true;

            if (address.IsIPv4MappedToIPv6)
                return IsRestricted(address.MapToIPv4());

            return false;
        }

        var octets = address.GetAddressBytes();
        return octets[0] switch
        {
            0 => true,
            10 => true,
            127 => true,
            169 when octets[1] == 254 => true,
            172 when octets[1] >= 16 && octets[1] <= 31 => true,
            192 when octets[1] == 168 => true,
            100 when octets[1] >= 64 && octets[1] <= 127 => true,
            _ => false
        };
    }
}
