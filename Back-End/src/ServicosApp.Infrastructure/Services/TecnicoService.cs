using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class TecnicoService : ITecnicoService
{
    private readonly AppDbContext _context;

    public TecnicoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TecnicoDto> CriarAsync(
        Guid empresaId,
        CreateTecnicoDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidarEmailDuplicadoAsync(empresaId, dto.Email, null, cancellationToken);

        var tecnico = new Tecnico
        {
            EmpresaId = empresaId,
            Nome = dto.Nome.Trim(),
            Telefone = string.IsNullOrWhiteSpace(dto.Telefone) ? null : dto.Telefone.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            Especialidade = string.IsNullOrWhiteSpace(dto.Especialidade) ? null : dto.Especialidade.Trim(),
            Observacoes = string.IsNullOrWhiteSpace(dto.Observacoes) ? null : dto.Observacoes.Trim(),
            Ativo = dto.Ativo
        };

        _context.Tecnicos.Add(tecnico);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(tecnico);
    }

    public async Task<List<TecnicoDto>> ListarAsync(
        Guid empresaId,
        bool? ativo = null,
        string? busca = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Tecnicos
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId);

        if (ativo.HasValue)
            query = query.Where(x => x.Ativo == ativo.Value);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = $"%{busca.Trim()}%";

            query = query.Where(x =>
                EF.Functions.Like(x.Nome, termo) ||
                (x.Email != null && EF.Functions.Like(x.Email, termo)) ||
                (x.Especialidade != null && EF.Functions.Like(x.Especialidade, termo)));
        }

        return await query
            .OrderBy(x => x.Nome)
            .Select(x => new TecnicoDto
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                Nome = x.Nome,
                Telefone = x.Telefone,
                Email = x.Email,
                Especialidade = x.Especialidade,
                Observacoes = x.Observacoes,
                Ativo = x.Ativo
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TecnicoDto?> ObterPorIdAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tecnico = await _context.Tecnicos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        return tecnico is null ? null : MapToDto(tecnico);
    }

    public async Task<TecnicoDto> AtualizarAsync(
        Guid empresaId,
        Guid id,
        UpdateTecnicoDto dto,
        CancellationToken cancellationToken = default)
    {
        var tecnico = await _context.Tecnicos
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (tecnico is null)
            throw new KeyNotFoundException("Técnico não encontrado.");

        await ValidarEmailDuplicadoAsync(empresaId, dto.Email, id, cancellationToken);

        tecnico.Nome = dto.Nome.Trim();
        tecnico.Telefone = string.IsNullOrWhiteSpace(dto.Telefone) ? null : dto.Telefone.Trim();
        tecnico.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        tecnico.Especialidade = string.IsNullOrWhiteSpace(dto.Especialidade) ? null : dto.Especialidade.Trim();
        tecnico.Observacoes = string.IsNullOrWhiteSpace(dto.Observacoes) ? null : dto.Observacoes.Trim();
        tecnico.Ativo = dto.Ativo;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(tecnico);
    }

    public async Task<bool> InativarAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tecnico = await _context.Tecnicos
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (tecnico is null)
            return false;

        tecnico.Ativo = false;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> AtivarAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tecnico = await _context.Tecnicos
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (tecnico is null)
            return false;

        tecnico.Ativo = true;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task ValidarEmailDuplicadoAsync(
        Guid empresaId,
        string? email,
        Guid? tecnicoId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        var emailTratado = email.Trim();

        var existe = await _context.Tecnicos.AnyAsync(
            x => x.EmpresaId == empresaId
                 && x.Email != null
                 && EF.Functions.Like(x.Email, emailTratado)
                 && (!tecnicoId.HasValue || x.Id != tecnicoId.Value),
            cancellationToken);

        if (existe)
            throw new ArgumentException("Já existe um técnico com este email nesta empresa.");
    }

    private static TecnicoDto MapToDto(Tecnico tecnico)
    {
        return new TecnicoDto
        {
            Id = tecnico.Id,
            EmpresaId = tecnico.EmpresaId,
            Nome = tecnico.Nome,
            Telefone = tecnico.Telefone,
            Email = tecnico.Email,
            Especialidade = tecnico.Especialidade,
            Observacoes = tecnico.Observacoes,
            Ativo = tecnico.Ativo
        };
    }
}