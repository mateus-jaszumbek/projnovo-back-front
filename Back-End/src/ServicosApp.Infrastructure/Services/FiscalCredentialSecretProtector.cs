using Microsoft.AspNetCore.DataProtection;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;

namespace ServicosApp.Infrastructure.Services;

public class FiscalCredentialSecretProtector : IFiscalCredentialSecretProtector
{
    private readonly IDataProtector _protector;

    public FiscalCredentialSecretProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("ServicosApp.FiscalCredentials.v1");
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
            // Compatibilidade com dados antigos que possam estar sem proteção.
            return value;
        }
    }

    public CredencialFiscalEmpresa CloneForUse(CredencialFiscalEmpresa entity)
    {
        return new CredencialFiscalEmpresa
        {
            Id = entity.Id,
            EmpresaId = entity.EmpresaId,
            TipoDocumentoFiscal = entity.TipoDocumentoFiscal,
            Provedor = entity.Provedor,
            UrlBase = entity.UrlBase,
            ClientId = entity.ClientId,
            ClientSecretEncrypted = Unprotect(entity.ClientSecretEncrypted),
            UsuarioApi = entity.UsuarioApi,
            SenhaApiEncrypted = Unprotect(entity.SenhaApiEncrypted),
            CertificadoBase64Encrypted = Unprotect(entity.CertificadoBase64Encrypted),
            CertificadoSenhaEncrypted = Unprotect(entity.CertificadoSenhaEncrypted),
            TokenAcesso = Unprotect(entity.TokenAcesso),
            TokenExpiraEm = entity.TokenExpiraEm,
            Ativo = entity.Ativo,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
