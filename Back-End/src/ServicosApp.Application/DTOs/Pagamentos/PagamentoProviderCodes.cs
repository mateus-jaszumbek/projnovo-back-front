namespace ServicosApp.Application.DTOs.Pagamentos;

public static class PagamentoProviderCodes
{
    public const string MercadoPago = "mercadopago";
    public const string Stone = "stone";
    public const string Cielo = "cielo";
    public const string PagBank = "pagbank";
    public const string GetNet = "getnet";
}

public static class PagamentoProviderCodeNormalizer
{
    public static string Normalize(string? providerCode)
        => (providerCode ?? string.Empty).Trim().ToLowerInvariant();
}
