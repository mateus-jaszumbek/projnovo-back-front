using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Domain.Entities;

namespace ServicosApp.Application.Interfaces;

public interface INfseProviderResolver
{
    INfseProviderClient Resolve(ConfiguracaoFiscal configuracaoFiscal, CredencialFiscalEmpresa credencial);
}
