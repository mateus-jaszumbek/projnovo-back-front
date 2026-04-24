using ServicosApp.Domain.Entities;

namespace ServicosApp.Application.Interfaces;

public interface IFiscalCredentialSecretProtector
{
    string? Protect(string? value);
    string? Unprotect(string? value);
    CredencialFiscalEmpresa CloneForUse(CredencialFiscalEmpresa entity);
}
