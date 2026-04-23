using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class PecaService : IPecaService
{
    private readonly AppDbContext _context;

    public PecaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PecaDto> CriarAsync(Guid empresaId, CreatePecaDto dto, CancellationToken cancellationToken = default)
    {
        ValidarCampos(dto);
        await ValidarDuplicidadeAsync(empresaId, dto.Nome, dto.CodigoInterno, dto.Sku, null, cancellationToken);
        await ValidarFornecedorAsync(empresaId, dto.FornecedorId, cancellationToken);

        var entity = new Peca
        {
            EmpresaId = empresaId,
            Nome = dto.Nome.Trim(),
            CodigoInterno = Normalizar(dto.CodigoInterno),
            Sku = Normalizar(dto.Sku),
            Descricao = Normalizar(dto.Descricao),
            Categoria = Normalizar(dto.Categoria),
            Marca = Normalizar(dto.Marca),
            ModeloCompativel = Normalizar(dto.ModeloCompativel),
            Ncm = Normalizar(dto.Ncm),
            Cest = Normalizar(dto.Cest),
            CfopPadraoNfe = Normalizar(dto.CfopPadraoNfe),
            CfopPadraoNfce = Normalizar(dto.CfopPadraoNfce),
            CstCsosn = Normalizar(dto.CstCsosn),
            OrigemMercadoria = Normalizar(dto.OrigemMercadoria),
            Unidade = string.IsNullOrWhiteSpace(dto.Unidade) ? "UN" : dto.Unidade.Trim(),
            FornecedorId = dto.FornecedorId,
            CustoUnitario = dto.CustoUnitario,
            PrecoVenda = dto.PrecoVenda,
            GarantiaDias = dto.GarantiaDias,
            EstoqueAtual = dto.EstoqueAtual,
            EstoqueMinimo = dto.EstoqueMinimo,
            Ativo = dto.Ativo
        };

        _context.Pecas.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<List<PecaDto>> ListarAsync(Guid empresaId, bool? ativo = null, string? busca = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Pecas
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId);

        if (ativo.HasValue)
            query = query.Where(x => x.Ativo == ativo.Value);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = $"%{busca.Trim()}%";

            query = query.Where(x =>
                EF.Functions.Like(x.Nome, termo) ||
                (x.CodigoInterno != null && EF.Functions.Like(x.CodigoInterno, termo)) ||
                (x.Sku != null && EF.Functions.Like(x.Sku, termo)) ||
                (x.Descricao != null && EF.Functions.Like(x.Descricao, termo)) ||
                (x.Categoria != null && EF.Functions.Like(x.Categoria, termo)) ||
                (x.Marca != null && EF.Functions.Like(x.Marca, termo)) ||
                (x.ModeloCompativel != null && EF.Functions.Like(x.ModeloCompativel, termo)));
        }

        return await query
            .Include(x => x.Fornecedor)
            .OrderBy(x => x.Nome)
            .Select(x => new PecaDto
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                Nome = x.Nome,
                CodigoInterno = x.CodigoInterno,
                Sku = x.Sku,
                Descricao = x.Descricao,
                Categoria = x.Categoria,
                Marca = x.Marca,
                ModeloCompativel = x.ModeloCompativel,
                Ncm = x.Ncm,
                Cest = x.Cest,
                CfopPadraoNfe = x.CfopPadraoNfe,
                CfopPadraoNfce = x.CfopPadraoNfce,
                CstCsosn = x.CstCsosn,
                OrigemMercadoria = x.OrigemMercadoria,
                Unidade = x.Unidade,
                FornecedorId = x.FornecedorId,
                FornecedorNome = x.Fornecedor == null ? null : x.Fornecedor.Nome,
                FornecedorWhatsApp = x.Fornecedor == null ? null : x.Fornecedor.WhatsApp,
                FornecedorEmail = x.Fornecedor == null ? null : x.Fornecedor.Email,
                FornecedorMensagemPadrao = x.Fornecedor == null ? null : x.Fornecedor.MensagemPadrao,
                CustoUnitario = x.CustoUnitario,
                PrecoVenda = x.PrecoVenda,
                GarantiaDias = x.GarantiaDias,
                EstoqueAtual = x.EstoqueAtual,
                EstoqueMinimo = x.EstoqueMinimo,
                Ativo = x.Ativo,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PecaDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Pecas
            .Include(x => x.Fornecedor)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<PecaDto?> AtualizarAsync(Guid empresaId, Guid id, UpdatePecaDto dto, CancellationToken cancellationToken = default)
    {
        ValidarCampos(dto);

        var entity = await _context.Pecas
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return null;

        await ValidarDuplicidadeAsync(empresaId, dto.Nome, dto.CodigoInterno, dto.Sku, id, cancellationToken);
        await ValidarFornecedorAsync(empresaId, dto.FornecedorId, cancellationToken);

        entity.Nome = dto.Nome.Trim();
        entity.CodigoInterno = Normalizar(dto.CodigoInterno);
        entity.Sku = Normalizar(dto.Sku);
        entity.Descricao = Normalizar(dto.Descricao);
        entity.Categoria = Normalizar(dto.Categoria);
        entity.Marca = Normalizar(dto.Marca);
        entity.ModeloCompativel = Normalizar(dto.ModeloCompativel);
        entity.Ncm = Normalizar(dto.Ncm);
        entity.Cest = Normalizar(dto.Cest);
        entity.CfopPadraoNfe = Normalizar(dto.CfopPadraoNfe);
        entity.CfopPadraoNfce = Normalizar(dto.CfopPadraoNfce);
        entity.CstCsosn = Normalizar(dto.CstCsosn);
        entity.OrigemMercadoria = Normalizar(dto.OrigemMercadoria);
        entity.Unidade = string.IsNullOrWhiteSpace(dto.Unidade) ? "UN" : dto.Unidade.Trim();
        entity.FornecedorId = dto.FornecedorId;
        entity.CustoUnitario = dto.CustoUnitario;
        entity.PrecoVenda = dto.PrecoVenda;
        entity.GarantiaDias = dto.GarantiaDias;
        entity.EstoqueAtual = dto.EstoqueAtual;
        entity.EstoqueMinimo = dto.EstoqueMinimo;
        entity.Ativo = dto.Ativo;

        await _context.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<bool> InativarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Pecas
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return false;

        entity.Ativo = false;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> AtivarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Pecas
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return false;

        entity.Ativo = true;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task ValidarDuplicidadeAsync(Guid empresaId, string nome, string? codigoInterno, string? sku, Guid? idIgnorar, CancellationToken cancellationToken)
    {
        var nomeTratado = nome.Trim();

        var nomeExiste = await _context.Pecas.AnyAsync(x =>
            x.EmpresaId == empresaId &&
            EF.Functions.Like(x.Nome, nomeTratado) &&
            (!idIgnorar.HasValue || x.Id != idIgnorar.Value),
            cancellationToken);

        if (nomeExiste)
            throw new InvalidOperationException("Já existe uma peça com este nome nesta empresa.");

        if (!string.IsNullOrWhiteSpace(codigoInterno))
        {
            var codigoTratado = codigoInterno.Trim();

            var codigoExiste = await _context.Pecas.AnyAsync(x =>
                x.EmpresaId == empresaId &&
                x.CodigoInterno != null &&
                EF.Functions.Like(x.CodigoInterno, codigoTratado) &&
                (!idIgnorar.HasValue || x.Id != idIgnorar.Value),
                cancellationToken);

            if (codigoExiste)
                throw new InvalidOperationException("Já existe uma peça com este código interno nesta empresa.");
        }

        if (!string.IsNullOrWhiteSpace(sku))
        {
            var skuTratado = sku.Trim();

            var skuExiste = await _context.Pecas.AnyAsync(x =>
                x.EmpresaId == empresaId &&
                x.Sku != null &&
                EF.Functions.Like(x.Sku, skuTratado) &&
                (!idIgnorar.HasValue || x.Id != idIgnorar.Value),
                cancellationToken);

            if (skuExiste)
                throw new InvalidOperationException("Já existe uma peça com este SKU nesta empresa.");
        }
    }

    private async Task ValidarFornecedorAsync(Guid empresaId, Guid? fornecedorId, CancellationToken cancellationToken)
    {
        if (!fornecedorId.HasValue)
            return;

        var existe = await _context.Fornecedores
            .AsNoTracking()
            .AnyAsync(x => x.EmpresaId == empresaId && x.Id == fornecedorId.Value && x.Ativo, cancellationToken);

        if (!existe)
            throw new InvalidOperationException("Fornecedor nao encontrado.");
    }

    private static void ValidarCampos(CreatePecaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new InvalidOperationException("Nome é obrigatório.");

        if (dto.CustoUnitario < 0)
            throw new InvalidOperationException("Custo unitário não pode ser negativo.");

        if (dto.PrecoVenda < 0)
            throw new InvalidOperationException("Preço de venda não pode ser negativo.");

        if (dto.GarantiaDias < 0)
            throw new InvalidOperationException("Garantia não pode ser negativa.");

        if (dto.EstoqueAtual < 0)
            throw new InvalidOperationException("Estoque atual não pode ser negativo.");

        if (dto.EstoqueMinimo < 0)
            throw new InvalidOperationException("Estoque mínimo não pode ser negativo.");

        if (string.IsNullOrWhiteSpace(dto.Unidade))
            throw new InvalidOperationException("Unidade é obrigatória.");
    }

    private static void ValidarCampos(UpdatePecaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new InvalidOperationException("Nome é obrigatório.");

        if (dto.CustoUnitario < 0)
            throw new InvalidOperationException("Custo unitário não pode ser negativo.");

        if (dto.PrecoVenda < 0)
            throw new InvalidOperationException("Preço de venda não pode ser negativo.");

        if (dto.GarantiaDias < 0)
            throw new InvalidOperationException("Garantia não pode ser negativa.");

        if (dto.EstoqueAtual < 0)
            throw new InvalidOperationException("Estoque atual não pode ser negativo.");

        if (dto.EstoqueMinimo < 0)
            throw new InvalidOperationException("Estoque mínimo não pode ser negativo.");

        if (string.IsNullOrWhiteSpace(dto.Unidade))
            throw new InvalidOperationException("Unidade é obrigatória.");
    }

    private static string? Normalizar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static PecaDto Map(Peca entity)
    {
        return new PecaDto
        {
            Id = entity.Id,
            EmpresaId = entity.EmpresaId,
            Nome = entity.Nome,
            CodigoInterno = entity.CodigoInterno,
            Sku = entity.Sku,
            Descricao = entity.Descricao,
            Categoria = entity.Categoria,
            Marca = entity.Marca,
            ModeloCompativel = entity.ModeloCompativel,
            Ncm = entity.Ncm,
            Cest = entity.Cest,
            CfopPadraoNfe = entity.CfopPadraoNfe,
            CfopPadraoNfce = entity.CfopPadraoNfce,
            CstCsosn = entity.CstCsosn,
            OrigemMercadoria = entity.OrigemMercadoria,
            Unidade = entity.Unidade,
            FornecedorId = entity.FornecedorId,
            FornecedorNome = entity.Fornecedor?.Nome,
            FornecedorWhatsApp = entity.Fornecedor?.WhatsApp,
            FornecedorEmail = entity.Fornecedor?.Email,
            FornecedorMensagemPadrao = entity.Fornecedor?.MensagemPadrao,
            CustoUnitario = entity.CustoUnitario,
            PrecoVenda = entity.PrecoVenda,
            GarantiaDias = entity.GarantiaDias,
            EstoqueAtual = entity.EstoqueAtual,
            EstoqueMinimo = entity.EstoqueMinimo,
            Ativo = entity.Ativo,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
