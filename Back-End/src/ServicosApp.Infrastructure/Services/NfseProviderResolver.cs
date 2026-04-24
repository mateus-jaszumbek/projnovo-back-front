using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;

namespace ServicosApp.Infrastructure.Services;

public class NfseProviderResolver : INfseProviderResolver
{
    private readonly IReadOnlyDictionary<string, INfseProviderClient> _providers;

    public NfseProviderResolver(IEnumerable<INfseProviderClient> providers)
    {
        _providers = providers
            .GroupBy(x => FiscalProviderCodeNormalizer.Normalize(x.ProviderCode), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last(), StringComparer.OrdinalIgnoreCase);
    }

    public INfseProviderClient Resolve(ConfiguracaoFiscal configuracaoFiscal, CredencialFiscalEmpresa credencial)
    {
        var providerCode = ResolveProviderCode(
            credencial.Provedor,
            configuracaoFiscal.ProvedorFiscal,
            _providers.Keys);

        if (_providers.TryGetValue(providerCode, out var provider))
            return provider;

        throw new InvalidOperationException(
            $"Provedor fiscal '{providerCode}' não está registrado para NFS-e. Registrados: {FormatRegisteredProviders(_providers.Keys)}.");
    }

    private static string ResolveProviderCode(
        string? providerFromCredential,
        string? providerFromConfiguration,
        IEnumerable<string> registeredProviders)
    {
        if (!string.IsNullOrWhiteSpace(providerFromCredential))
            return FiscalProviderCodeNormalizer.Normalize(providerFromCredential);

        if (!string.IsNullOrWhiteSpace(providerFromConfiguration))
            return FiscalProviderCodeNormalizer.Normalize(providerFromConfiguration);

        var registered = registeredProviders
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (registered.Any(FiscalProviderCodeNormalizer.IsFake))
            return FiscalProviderCodes.Fake;

        if (registered.Length == 1)
            return registered[0];

        throw new InvalidOperationException(
            "Nenhum provedor fiscal foi configurado para NFS-e. Configure o provedor na credencial fiscal da empresa ou na configuração fiscal.");
    }

    private static string FormatRegisteredProviders(IEnumerable<string> providers)
        => string.Join(", ", providers.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
}
