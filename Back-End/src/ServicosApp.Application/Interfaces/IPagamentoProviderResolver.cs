using ServicosApp.Application.DTOs.Pagamentos;

namespace ServicosApp.Application.Interfaces;

public interface IPagamentoProviderResolver
{
    IPagamentoProviderClient Resolve(string providerCode);

    IReadOnlyCollection<PagamentoProviderInfoDto> ListarDisponiveis();
}
