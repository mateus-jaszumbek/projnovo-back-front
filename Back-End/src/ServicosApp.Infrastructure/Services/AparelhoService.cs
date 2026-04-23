using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class AparelhoService : IAparelhoService
{
    private readonly AppDbContext _context;

    public AparelhoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AparelhoDto> CriarAsync(
        Guid empresaId,
        CreateAparelhoDto dto,
        CancellationToken cancellationToken = default)
    {
        var empresaExiste = await _context.Empresas
            .AnyAsync(x => x.Id == empresaId && x.Ativo, cancellationToken);

        if (!empresaExiste)
            throw new InvalidOperationException("Empresa não encontrada.");

        var clienteExiste = await _context.Clientes
            .AnyAsync(x => x.Id == dto.ClienteId && x.EmpresaId == empresaId && x.Ativo, cancellationToken);

        if (!clienteExiste)
            throw new InvalidOperationException("Cliente não encontrado para a empresa informada.");

        if (string.IsNullOrWhiteSpace(dto.Marca))
            throw new InvalidOperationException("Marca é obrigatória.");

        if (string.IsNullOrWhiteSpace(dto.Modelo))
            throw new InvalidOperationException("Modelo é obrigatório.");

        var aparelho = new Aparelho
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            ClienteId = dto.ClienteId,
            Marca = dto.Marca.Trim(),
            Modelo = dto.Modelo.Trim(),
            Cor = dto.Cor?.Trim(),
            Imei = dto.Imei?.Trim(),
            SerialNumber = dto.SerialNumber?.Trim(),
            SenhaAparelho = dto.SenhaAparelho?.Trim(),
            Acessorios = dto.Acessorios?.Trim(),
            EstadoFisico = dto.EstadoFisico?.Trim(),
            Observacoes = dto.Observacoes?.Trim(),
            Ativo = true
        };

        _context.Aparelhos.Add(aparelho);
        await _context.SaveChangesAsync(cancellationToken);

        return Map(aparelho);
    }

    public async Task<List<AparelhoDto>> ListarAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        return await BaseQuery(empresaId)
            .OrderBy(x => x.Marca)
            .ThenBy(x => x.Modelo)
            .Select(MapExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AparelhoDto>> ListarPorClienteAsync(
        Guid empresaId,
        Guid clienteId,
        CancellationToken cancellationToken = default)
    {
        return await BaseQuery(empresaId)
            .Where(x => x.ClienteId == clienteId)
            .OrderBy(x => x.Marca)
            .ThenBy(x => x.Modelo)
            .Select(MapExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<AparelhoDto?> ObterPorIdAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var aparelho = await _context.Aparelhos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == empresaId, cancellationToken);

        return aparelho is null ? null : Map(aparelho);
    }

    public async Task<AparelhoDto?> AtualizarAsync(
        Guid empresaId,
        Guid id,
        UpdateAparelhoDto dto,
        CancellationToken cancellationToken = default)
    {
        var aparelho = await _context.Aparelhos
            .FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == empresaId, cancellationToken);

        if (aparelho is null)
            return null;

        var clienteExiste = await _context.Clientes
            .AnyAsync(x => x.Id == dto.ClienteId && x.EmpresaId == empresaId && x.Ativo, cancellationToken);

        if (!clienteExiste)
            throw new InvalidOperationException("Cliente não encontrado para a empresa informada.");

        if (string.IsNullOrWhiteSpace(dto.Marca))
            throw new InvalidOperationException("Marca é obrigatória.");

        if (string.IsNullOrWhiteSpace(dto.Modelo))
            throw new InvalidOperationException("Modelo é obrigatório.");

        aparelho.ClienteId = dto.ClienteId;
        aparelho.Marca = dto.Marca.Trim();
        aparelho.Modelo = dto.Modelo.Trim();
        aparelho.Cor = dto.Cor?.Trim();
        aparelho.Imei = dto.Imei?.Trim();
        aparelho.SerialNumber = dto.SerialNumber?.Trim();
        aparelho.SenhaAparelho = dto.SenhaAparelho?.Trim();
        aparelho.Acessorios = dto.Acessorios?.Trim();
        aparelho.EstadoFisico = dto.EstadoFisico?.Trim();
        aparelho.Observacoes = dto.Observacoes?.Trim();
        aparelho.Ativo = dto.Ativo;

        await _context.SaveChangesAsync(cancellationToken);

        return Map(aparelho);
    }

    public async Task<bool> InativarAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var aparelho = await _context.Aparelhos
            .FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == empresaId, cancellationToken);

        if (aparelho is null)
            return false;

        aparelho.Ativo = false;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<Aparelho> BaseQuery(Guid empresaId)
    {
        return _context.Aparelhos
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Ativo);
    }

    private static System.Linq.Expressions.Expression<Func<Aparelho, AparelhoDto>> MapExpression()
    {
        return x => new AparelhoDto
        {
            Id = x.Id,
            EmpresaId = x.EmpresaId,
            ClienteId = x.ClienteId,
            Marca = x.Marca,
            Modelo = x.Modelo,
            Cor = x.Cor,
            Imei = x.Imei,
            SerialNumber = x.SerialNumber,
            SenhaAparelho = x.SenhaAparelho,
            Acessorios = x.Acessorios,
            EstadoFisico = x.EstadoFisico,
            Observacoes = x.Observacoes,
            Ativo = x.Ativo,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }

    private static AparelhoDto Map(Aparelho aparelho)
    {
        return new AparelhoDto
        {
            Id = aparelho.Id,
            EmpresaId = aparelho.EmpresaId,
            ClienteId = aparelho.ClienteId,
            Marca = aparelho.Marca,
            Modelo = aparelho.Modelo,
            Cor = aparelho.Cor,
            Imei = aparelho.Imei,
            SerialNumber = aparelho.SerialNumber,
            SenhaAparelho = aparelho.SenhaAparelho,
            Acessorios = aparelho.Acessorios,
            EstadoFisico = aparelho.EstadoFisico,
            Observacoes = aparelho.Observacoes,
            Ativo = aparelho.Ativo,
            CreatedAt = aparelho.CreatedAt,
            UpdatedAt = aparelho.UpdatedAt
        };
    }
}
