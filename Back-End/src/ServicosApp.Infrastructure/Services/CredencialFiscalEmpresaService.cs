using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Application.Exceptions;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class CredencialFiscalEmpresaService : ICredencialFiscalEmpresaService
{
    private readonly AppDbContext _context;
    private readonly IFiscalCredentialSecretProtector _secretProtector;

    public CredencialFiscalEmpresaService(
        AppDbContext context,
        IFiscalCredentialSecretProtector secretProtector)
    {
        _context = context;
        _secretProtector = secretProtector;
    }

    public async Task<CredencialFiscalEmpresaDto> CriarAsync(
        Guid empresaId,
        CreateCredencialFiscalEmpresaDto dto,
        CancellationToken cancellationToken = default)
    {
        Validar(dto.TipoDocumentoFiscal, dto.Provedor);

        var tipoDocumento = ParseTipoDocumento(dto.TipoDocumentoFiscal);
        var provedor = FiscalProviderCodeNormalizer.Normalize(dto.Provedor);

        var credenciaisExistentes = await _context.CredenciaisFiscaisEmpresas
            .AsNoTracking()
            .Where(
                x => x.EmpresaId == empresaId &&
                     x.TipoDocumentoFiscal == tipoDocumento)
            .ToListAsync(cancellationToken);

        var jaExiste = credenciaisExistentes.Any(
            x => string.Equals(
                FiscalProviderCodeNormalizer.NormalizeOrNull(x.Provedor),
                provedor,
                StringComparison.OrdinalIgnoreCase));

        if (jaExiste)
            throw new AppConflictException("Ja existe credencial fiscal para esse documento e provedor.");

        var entity = new CredencialFiscalEmpresa
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            TipoDocumentoFiscal = tipoDocumento,
            Provedor = provedor,
            UrlBase = Normalizar(dto.UrlBase),
            ClientId = Normalizar(dto.ClientId),
            ClientSecretEncrypted = _secretProtector.Protect(dto.ClientSecret),
            UsuarioApi = Normalizar(dto.UsuarioApi),
            SenhaApiEncrypted = _secretProtector.Protect(dto.SenhaApi),
            CertificadoBase64Encrypted = _secretProtector.Protect(dto.CertificadoBase64),
            CertificadoSenhaEncrypted = _secretProtector.Protect(dto.CertificadoSenha),
            TokenAcesso = _secretProtector.Protect(dto.TokenAcesso),
            TokenExpiraEm = dto.TokenExpiraEm,
            Ativo = dto.Ativo
        };

        _context.CredenciaisFiscaisEmpresas.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<List<CredencialFiscalEmpresaDto>> ListarAsync(
        Guid empresaId,
        string? tipoDocumentoFiscal,
        bool? ativo,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CredenciaisFiscaisEmpresas
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId);

        if (!string.IsNullOrWhiteSpace(tipoDocumentoFiscal))
        {
            var tipo = ParseTipoDocumento(tipoDocumentoFiscal);
            query = query.Where(x => x.TipoDocumentoFiscal == tipo);
        }

        if (ativo.HasValue)
            query = query.Where(x => x.Ativo == ativo.Value);

        var credenciais = await query
            .OrderBy(x => x.TipoDocumentoFiscal)
            .ThenBy(x => x.Provedor)
            .ToListAsync(cancellationToken);

        return credenciais.Select(Map).ToList();
    }

    public async Task<CredencialFiscalEmpresaDto?> ObterPorIdAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.CredenciaisFiscaisEmpresas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<CredencialFiscalEmpresaDto?> AtualizarAsync(
        Guid empresaId,
        Guid id,
        UpdateCredencialFiscalEmpresaDto dto,
        CancellationToken cancellationToken = default)
    {
        Validar(dto.TipoDocumentoFiscal, dto.Provedor);

        var entity = await _context.CredenciaisFiscaisEmpresas
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return null;

        var tipoDocumento = ParseTipoDocumento(dto.TipoDocumentoFiscal);
        var provedor = FiscalProviderCodeNormalizer.Normalize(dto.Provedor);

        var credenciaisExistentes = await _context.CredenciaisFiscaisEmpresas
            .AsNoTracking()
            .Where(
                x => x.EmpresaId == empresaId &&
                     x.Id != id &&
                     x.TipoDocumentoFiscal == tipoDocumento)
            .ToListAsync(cancellationToken);

        var conflito = credenciaisExistentes.Any(
            x => string.Equals(
                FiscalProviderCodeNormalizer.NormalizeOrNull(x.Provedor),
                provedor,
                StringComparison.OrdinalIgnoreCase));

        if (conflito)
            throw new AppConflictException("Ja existe credencial fiscal para esse documento e provedor.");

        entity.TipoDocumentoFiscal = tipoDocumento;
        entity.Provedor = provedor;
        entity.UrlBase = Normalizar(dto.UrlBase);
        entity.ClientId = Normalizar(dto.ClientId);
        entity.UsuarioApi = Normalizar(dto.UsuarioApi);
        entity.TokenExpiraEm = dto.LimparTokenAcesso ? null : dto.TokenExpiraEm ?? entity.TokenExpiraEm;
        entity.Ativo = dto.Ativo;

        if (dto.LimparClientSecret)
            entity.ClientSecretEncrypted = null;
        else if (!string.IsNullOrWhiteSpace(dto.ClientSecret))
            entity.ClientSecretEncrypted = _secretProtector.Protect(dto.ClientSecret);

        if (dto.LimparSenhaApi)
            entity.SenhaApiEncrypted = null;
        else if (!string.IsNullOrWhiteSpace(dto.SenhaApi))
            entity.SenhaApiEncrypted = _secretProtector.Protect(dto.SenhaApi);

        if (dto.LimparCertificado)
        {
            entity.CertificadoBase64Encrypted = null;
            entity.CertificadoSenhaEncrypted = null;
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(dto.CertificadoBase64))
                entity.CertificadoBase64Encrypted = _secretProtector.Protect(dto.CertificadoBase64);

            if (!string.IsNullOrWhiteSpace(dto.CertificadoSenha))
                entity.CertificadoSenhaEncrypted = _secretProtector.Protect(dto.CertificadoSenha);
        }

        if (dto.LimparTokenAcesso)
        {
            entity.TokenAcesso = null;
            entity.TokenExpiraEm = null;
        }
        else if (!string.IsNullOrWhiteSpace(dto.TokenAcesso))
        {
            entity.TokenAcesso = _secretProtector.Protect(dto.TokenAcesso);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    private static CredencialFiscalEmpresaDto Map(CredencialFiscalEmpresa entity)
    {
        return new CredencialFiscalEmpresaDto
        {
            Id = entity.Id,
            EmpresaId = entity.EmpresaId,
            TipoDocumentoFiscal = entity.TipoDocumentoFiscal.ToString(),
            Provedor = entity.Provedor,
            UrlBase = entity.UrlBase,
            ClientId = entity.ClientId,
            UsuarioApi = entity.UsuarioApi,
            ClientSecretConfigurado = !string.IsNullOrWhiteSpace(entity.ClientSecretEncrypted),
            SenhaApiConfigurada = !string.IsNullOrWhiteSpace(entity.SenhaApiEncrypted),
            CertificadoConfigurado = !string.IsNullOrWhiteSpace(entity.CertificadoBase64Encrypted),
            CertificadoSenhaConfigurada = !string.IsNullOrWhiteSpace(entity.CertificadoSenhaEncrypted),
            TokenAcessoConfigurado = !string.IsNullOrWhiteSpace(entity.TokenAcesso),
            TokenExpiraEm = entity.TokenExpiraEm,
            Ativo = entity.Ativo,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static void Validar(string? tipoDocumentoFiscal, string? provedor)
    {
        _ = ParseTipoDocumento(tipoDocumentoFiscal);

        if (string.IsNullOrWhiteSpace(provedor))
            throw new AppValidationException("Provedor fiscal e obrigatorio.");
    }

    private static TipoDocumentoFiscal ParseTipoDocumento(string? tipoDocumentoFiscal)
    {
        if (!Enum.TryParse<TipoDocumentoFiscal>(tipoDocumentoFiscal?.Trim(), true, out var tipo))
            throw new AppValidationException("Tipo de documento fiscal invalido.");

        return tipo;
    }

    private static string? Normalizar(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
