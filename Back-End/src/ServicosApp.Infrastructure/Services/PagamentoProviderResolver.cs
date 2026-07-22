using ServicosApp.Application.DTOs.Pagamentos;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.Infrastructure.Services;

public class PagamentoProviderResolver : IPagamentoProviderResolver
{
    // Catálogo de marcas conhecidas. "Implementado = false" aparece no cadastro da loja
    // como "em breve" - para ativar de verdade, crie uma classe IPagamentoProviderClient
    // para a marca e registre no DI (Program.cs).
    private static readonly PagamentoProviderInfoDto[] Catalogo =
    [
        new() { Codigo = PagamentoProviderCodes.MercadoPago, Nome = "Mercado Pago", Implementado = true, SuportaMaquininha = true, SuportaPix = true },
        new() { Codigo = PagamentoProviderCodes.Stone, Nome = "Stone", Implementado = false, SuportaMaquininha = true, SuportaPix = false },
        new() { Codigo = PagamentoProviderCodes.Cielo, Nome = "Cielo", Implementado = false, SuportaMaquininha = true, SuportaPix = false },
        new() { Codigo = PagamentoProviderCodes.PagBank, Nome = "PagBank (PagSeguro)", Implementado = false, SuportaMaquininha = true, SuportaPix = false },
        new() { Codigo = PagamentoProviderCodes.GetNet, Nome = "GetNet", Implementado = false, SuportaMaquininha = true, SuportaPix = false }
    ];

    private readonly IReadOnlyDictionary<string, IPagamentoProviderClient> _providers;

    public PagamentoProviderResolver(IEnumerable<IPagamentoProviderClient> providers)
    {
        _providers = providers
            .GroupBy(x => PagamentoProviderCodeNormalizer.Normalize(x.ProviderCode), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last(), StringComparer.OrdinalIgnoreCase);
    }

    public IPagamentoProviderClient Resolve(string providerCode)
    {
        var normalized = PagamentoProviderCodeNormalizer.Normalize(providerCode);

        if (_providers.TryGetValue(normalized, out var provider))
            return provider;

        throw new InvalidOperationException(
            $"Provedor de pagamento '{providerCode}' ainda não está implementado. Disponíveis: " +
            string.Join(", ", _providers.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) + ".");
    }

    public IReadOnlyCollection<PagamentoProviderInfoDto> ListarDisponiveis() => Catalogo;
}
