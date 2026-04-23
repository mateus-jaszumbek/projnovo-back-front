using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class ServicoCatalogoService : IServicoCatalogoService
{
    private readonly AppDbContext _context;

    public ServicoCatalogoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ServicoCatalogoDto> CriarAsync(
        Guid empresaId,
        CreateServicoCatalogoDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidarCampos(dto.Nome, dto.ValorPadrao, dto.TempoEstimadoMinutos, dto.GarantiaDias);

        await ValidarDuplicidadeAsync(empresaId, dto.Nome, dto.CodigoInterno, null, cancellationToken);

        var entity = new ServicoCatalogo
        {
            EmpresaId = empresaId,
            Nome = dto.Nome.Trim(),
            Descricao = Normalizar(dto.Descricao),
            ValorPadrao = dto.ValorPadrao,
            CodigoInterno = Normalizar(dto.CodigoInterno),
            TempoEstimadoMinutos = dto.TempoEstimadoMinutos,
            GarantiaDias = dto.GarantiaDias,
            Ativo = dto.Ativo
        };

        _context.ServicosCatalogo.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<List<ServicoCatalogoDto>> ListarAsync(
        Guid empresaId,
        bool? ativo = null,
        string? busca = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ServicosCatalogo
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId);

        if (ativo.HasValue)
            query = query.Where(x => x.Ativo == ativo.Value);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = $"%{busca.Trim()}%";

            query = query.Where(x =>
                EF.Functions.Like(x.Nome, termo) ||
                (x.Descricao != null && EF.Functions.Like(x.Descricao, termo)) ||
                (x.CodigoInterno != null && EF.Functions.Like(x.CodigoInterno, termo)));
        }

        return await query
            .OrderBy(x => x.Nome)
            .Select(x => new ServicoCatalogoDto
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                Nome = x.Nome,
                Descricao = x.Descricao,
                ValorPadrao = x.ValorPadrao,
                CodigoInterno = x.CodigoInterno,
                TempoEstimadoMinutos = x.TempoEstimadoMinutos,
                GarantiaDias = x.GarantiaDias,
                Ativo = x.Ativo,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ServicoCatalogoDto?> ObterPorIdAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.ServicosCatalogo
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<ServicoCatalogoDto?> AtualizarAsync(
        Guid empresaId,
        Guid id,
        UpdateServicoCatalogoDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidarCampos(dto.Nome, dto.ValorPadrao, dto.TempoEstimadoMinutos, dto.GarantiaDias);

        var entity = await _context.ServicosCatalogo
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return null;

        await ValidarDuplicidadeAsync(empresaId, dto.Nome, dto.CodigoInterno, id, cancellationToken);

        entity.Nome = dto.Nome.Trim();
        entity.Descricao = Normalizar(dto.Descricao);
        entity.ValorPadrao = dto.ValorPadrao;
        entity.CodigoInterno = Normalizar(dto.CodigoInterno);
        entity.TempoEstimadoMinutos = dto.TempoEstimadoMinutos;
        entity.GarantiaDias = dto.GarantiaDias;
        entity.Ativo = dto.Ativo;

        await _context.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<bool> InativarAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.ServicosCatalogo
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return false;

        entity.Ativo = false;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> AtivarAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.ServicosCatalogo
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return false;

        entity.Ativo = true;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task ValidarDuplicidadeAsync(
        Guid empresaId,
        string nome,
        string? codigoInterno,
        Guid? idIgnorar,
        CancellationToken cancellationToken)
    {
        var nomeTratado = nome.Trim();

        var nomeExiste = await _context.ServicosCatalogo.AnyAsync(x =>
            x.EmpresaId == empresaId &&
            EF.Functions.Like(x.Nome, nomeTratado) &&
            (!idIgnorar.HasValue || x.Id != idIgnorar.Value),
            cancellationToken);

        if (nomeExiste)
            throw new InvalidOperationException("Já existe um serviço com este nome nesta empresa.");

        if (!string.IsNullOrWhiteSpace(codigoInterno))
        {
            var codigoTratado = codigoInterno.Trim();

            var codigoExiste = await _context.ServicosCatalogo.AnyAsync(x =>
                x.EmpresaId == empresaId &&
                x.CodigoInterno != null &&
                EF.Functions.Like(x.CodigoInterno, codigoTratado) &&
                (!idIgnorar.HasValue || x.Id != idIgnorar.Value),
                cancellationToken);

            if (codigoExiste)
                throw new InvalidOperationException("Já existe um serviço com este código interno nesta empresa.");
        }
    }

    private static void ValidarCampos(string nome, decimal valorPadrao, int? tempoEstimadoMinutos, int garantiaDias)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new InvalidOperationException("Nome é obrigatório.");

        if (valorPadrao < 0)
            throw new InvalidOperationException("Valor padrão não pode ser negativo.");

        if (tempoEstimadoMinutos.HasValue && tempoEstimadoMinutos.Value <= 0)
            throw new InvalidOperationException("Tempo estimado deve ser maior que zero.");

        if (garantiaDias < 0)
            throw new InvalidOperationException("Garantia não pode ser negativa.");
    }

    private static string? Normalizar(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }

    private static ServicoCatalogoDto Map(ServicoCatalogo entity)
    {
        return new ServicoCatalogoDto
        {
            Id = entity.Id,
            EmpresaId = entity.EmpresaId,
            Nome = entity.Nome,
            Descricao = entity.Descricao,
            ValorPadrao = entity.ValorPadrao,
            CodigoInterno = entity.CodigoInterno,
            TempoEstimadoMinutos = entity.TempoEstimadoMinutos,
            GarantiaDias = entity.GarantiaDias,
            Ativo = entity.Ativo,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}