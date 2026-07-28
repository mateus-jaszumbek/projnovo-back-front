using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class CategoriaPecaService : ICategoriaPecaService
{
    private readonly AppDbContext _context;

    public CategoriaPecaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CategoriaPecaDto> CriarAsync(Guid empresaId, CreateCategoriaPecaDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new InvalidOperationException("Nome é obrigatório.");

        await ValidarDuplicidadeAsync(empresaId, dto.Nome, null, cancellationToken);

        var entity = new CategoriaPeca
        {
            EmpresaId = empresaId,
            Nome = dto.Nome.Trim(),
            Ativo = dto.Ativo
        };

        _context.CategoriasPeca.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<List<CategoriaPecaDto>> ListarAsync(
        Guid empresaId,
        bool? ativo = null,
        string? busca = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CategoriasPeca
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId);

        if (ativo.HasValue)
            query = query.Where(x => x.Ativo == ativo.Value);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = $"%{busca.Trim()}%";
            query = query.Where(x => EF.Functions.Like(x.Nome, termo));
        }

        return await query
            .OrderBy(x => x.Nome)
            .Select(x => new CategoriaPecaDto
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                Nome = x.Nome,
                Ativo = x.Ativo,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoriaPecaDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.CategoriasPeca
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<CategoriaPecaDto?> AtualizarAsync(Guid empresaId, Guid id, UpdateCategoriaPecaDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new InvalidOperationException("Nome é obrigatório.");

        var entity = await _context.CategoriasPeca
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return null;

        await ValidarDuplicidadeAsync(empresaId, dto.Nome, id, cancellationToken);

        entity.Nome = dto.Nome.Trim();
        entity.Ativo = dto.Ativo;

        await _context.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<bool> InativarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.CategoriasPeca
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return false;

        entity.Ativo = false;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> AtivarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.CategoriasPeca
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return false;

        entity.Ativo = true;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task ValidarDuplicidadeAsync(Guid empresaId, string nome, Guid? idIgnorar, CancellationToken cancellationToken)
    {
        var nomeTratado = nome.Trim();

        var nomeExiste = await _context.CategoriasPeca.AnyAsync(x =>
            x.EmpresaId == empresaId &&
            EF.Functions.Like(x.Nome, nomeTratado) &&
            (!idIgnorar.HasValue || x.Id != idIgnorar.Value),
            cancellationToken);

        if (nomeExiste)
            throw new InvalidOperationException("Já existe uma categoria com este nome nesta empresa.");
    }

    private static CategoriaPecaDto Map(CategoriaPeca entity)
    {
        return new CategoriaPecaDto
        {
            Id = entity.Id,
            EmpresaId = entity.EmpresaId,
            Nome = entity.Nome,
            Ativo = entity.Ativo,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
