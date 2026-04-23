using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Exceptions;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class RegraFiscalProdutoService : IRegraFiscalProdutoService
{
    private readonly AppDbContext _context;

    public RegraFiscalProdutoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RegraFiscalProdutoDto> CriarAsync(
        Guid empresaId,
        CreateRegraFiscalProdutoDto dto,
        CancellationToken cancellationToken = default)
    {
        Validar(dto);

        var entity = new RegraFiscalProduto
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId
        };

        Aplicar(entity, dto);

        _context.RegrasFiscaisProdutos.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<List<RegraFiscalProdutoDto>> ListarAsync(
        Guid empresaId,
        string? tipoDocumentoFiscal,
        bool? ativo,
        CancellationToken cancellationToken = default)
    {
        var query = _context.RegrasFiscaisProdutos
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId);

        if (!string.IsNullOrWhiteSpace(tipoDocumentoFiscal))
        {
            var tipo = ParseTipoDocumento(tipoDocumentoFiscal);
            query = query.Where(x => x.TipoDocumentoFiscal == tipo);
        }

        if (ativo.HasValue)
            query = query.Where(x => x.Ativo == ativo.Value);

        var regras = await query
            .OrderBy(x => x.TipoDocumentoFiscal)
            .ThenBy(x => x.UfOrigem)
            .ThenBy(x => x.UfDestino)
            .ThenBy(x => x.Ncm)
            .ToListAsync(cancellationToken);

        return regras.Select(Map).ToList();
    }

    public async Task<RegraFiscalProdutoDto?> ObterPorIdAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.RegrasFiscaisProdutos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<RegraFiscalProdutoDto?> AtualizarAsync(
        Guid empresaId,
        Guid id,
        UpdateRegraFiscalProdutoDto dto,
        CancellationToken cancellationToken = default)
    {
        Validar(dto);

        var entity = await _context.RegrasFiscaisProdutos
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return null;

        Aplicar(entity, dto);
        await _context.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    private static void Aplicar(RegraFiscalProduto entity, CreateRegraFiscalProdutoDto dto)
    {
        entity.TipoDocumentoFiscal = ParseTipoDocumento(dto.TipoDocumentoFiscal);
        entity.UfOrigem = NormalizarUf(dto.UfOrigem);
        entity.UfDestino = NormalizarUf(dto.UfDestino);
        entity.RegimeTributario = Normalizar(dto.RegimeTributario);
        entity.Ncm = Normalizar(dto.Ncm);
        entity.Cfop = dto.Cfop.Trim();
        entity.CstCsosn = dto.CstCsosn.Trim();
        entity.Cest = Normalizar(dto.Cest);
        entity.OrigemMercadoria = dto.OrigemMercadoria.Trim();
        entity.AliquotaIcms = dto.AliquotaIcms;
        entity.AliquotaPis = dto.AliquotaPis;
        entity.AliquotaCofins = dto.AliquotaCofins;
        entity.Ativo = dto.Ativo;
        entity.Observacoes = Normalizar(dto.Observacoes);
    }

    private static void Validar(CreateRegraFiscalProdutoDto dto)
    {
        var tipo = ParseTipoDocumento(dto.TipoDocumentoFiscal);
        if (tipo == TipoDocumentoFiscal.Nfse)
            throw new AppValidationException("Regra fiscal de produto deve ser para NF-e ou NFC-e.");

        if (string.IsNullOrWhiteSpace(dto.Cfop))
            throw new AppValidationException("CFOP é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.CstCsosn))
            throw new AppValidationException("CST/CSOSN é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.OrigemMercadoria))
            throw new AppValidationException("Origem da mercadoria é obrigatória.");
    }

    private static TipoDocumentoFiscal ParseTipoDocumento(string? tipoDocumentoFiscal)
    {
        if (!Enum.TryParse<TipoDocumentoFiscal>(tipoDocumentoFiscal?.Trim(), true, out var tipo))
            throw new AppValidationException("Tipo de documento fiscal inválido. Use Nfe ou Nfce.");

        return tipo;
    }

    private static RegraFiscalProdutoDto Map(RegraFiscalProduto entity)
    {
        return new RegraFiscalProdutoDto
        {
            Id = entity.Id,
            EmpresaId = entity.EmpresaId,
            TipoDocumentoFiscal = entity.TipoDocumentoFiscal.ToString(),
            UfOrigem = entity.UfOrigem,
            UfDestino = entity.UfDestino,
            RegimeTributario = entity.RegimeTributario,
            Ncm = entity.Ncm,
            Cfop = entity.Cfop,
            CstCsosn = entity.CstCsosn,
            Cest = entity.Cest,
            OrigemMercadoria = entity.OrigemMercadoria,
            AliquotaIcms = entity.AliquotaIcms,
            AliquotaPis = entity.AliquotaPis,
            AliquotaCofins = entity.AliquotaCofins,
            Ativo = entity.Ativo,
            Observacoes = entity.Observacoes,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static string? Normalizar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string? NormalizarUf(string? uf)
        => string.IsNullOrWhiteSpace(uf) ? null : uf.Trim().ToUpperInvariant();
}
