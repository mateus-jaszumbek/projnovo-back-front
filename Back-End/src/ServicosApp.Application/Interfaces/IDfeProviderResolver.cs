using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Domain.Entities;

namespace ServicosApp.Application.Interfaces;

public interface IDfeProviderResolver
{
    IDfeProviderClient Resolve(ConfiguracaoFiscal configuracaoFiscal, CredencialFiscalEmpresa? credencial);
}
