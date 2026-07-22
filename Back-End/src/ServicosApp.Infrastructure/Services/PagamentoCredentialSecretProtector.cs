using Microsoft.AspNetCore.DataProtection;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.Infrastructure.Services;

public class PagamentoCredentialSecretProtector : IPagamentoCredentialSecretProtector
{
    private readonly IDataProtector _protector;

    public PagamentoCredentialSecretProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("ServicosApp.PagamentoCredentials.v1");
    }

    public string? Protect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return _protector.Protect(value.Trim());
    }

    public string? Unprotect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            return _protector.Unprotect(value);
        }
        catch
        {
            return value;
        }
    }
}
