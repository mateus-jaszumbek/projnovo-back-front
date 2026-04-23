using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class CaixaDiarioService : ICaixaDiarioService
{
    private readonly AppDbContext _context;

    public CaixaDiarioService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CaixaDiarioDto> AbrirAsync(Guid empresaId, Guid? usuarioId, CreateCaixaDiarioDto dto, CancellationToken cancellationToken = default)
    {
        var existe = await _context.CaixasDiarios
            .AnyAsync(x => x.EmpresaId == empresaId && x.DataCaixa == dto.DataCaixa && x.Ativo, cancellationToken);

        if (existe)
            throw new InvalidOperationException("Já existe um caixa para esta data.");

        var entity = new CaixaDiario
        {
            EmpresaId = empresaId,
            DataCaixa = dto.DataCaixa,
            ValorAbertura = dto.ValorAbertura,
            ValorFechamentoSistema = dto.ValorAbertura,
            ValorFechamentoInformado = null,
            Diferenca = null,
            Status = "ABERTO",
            AbertoPor = usuarioId,
            FechadoPor = null,
            Ativo = true,
            DataAbertura = DateTime.UtcNow,
            DataFechamento = null,
            Observacoes = string.IsNullOrWhiteSpace(dto.Observacoes) ? null : dto.Observacoes.Trim()
        };

        _context.CaixasDiarios.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<List<CaixaDiarioDto>> ListarAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        return await _context.CaixasDiarios
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .OrderByDescending(x => x.DataCaixa)
            .Select(x => new CaixaDiarioDto
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                DataCaixa = x.DataCaixa,
                ValorAbertura = x.ValorAbertura,
                ValorFechamentoSistema = x.ValorFechamentoSistema,
                ValorFechamentoInformado = x.ValorFechamentoInformado,
                Diferenca = x.Diferenca,
                Status = x.Status,
                AbertoPor = x.AbertoPor,
                FechadoPor = x.FechadoPor,
                Ativo = x.Ativo,
                DataAbertura = x.DataAbertura,
                DataFechamento = x.DataFechamento,
                Observacoes = x.Observacoes
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CaixaDiarioDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.CaixasDiarios
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<CaixaDiarioDto?> FecharAsync(Guid empresaId, Guid id, Guid? usuarioId, FecharCaixaDiarioDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.CaixasDiarios
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return null;

        if (entity.Status == "FECHADO")
            throw new InvalidOperationException("Este caixa já está fechado.");

        entity.ValorFechamentoInformado = dto.ValorFechamentoInformado;
        entity.Diferenca = dto.ValorFechamentoInformado - entity.ValorFechamentoSistema;
        entity.Status = "FECHADO";
        entity.FechadoPor = usuarioId;
        entity.DataFechamento = DateTime.UtcNow;
        entity.Observacoes = string.IsNullOrWhiteSpace(dto.Observacoes)
            ? entity.Observacoes
            : dto.Observacoes.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    private static CaixaDiarioDto Map(CaixaDiario entity)
    {
        return new CaixaDiarioDto
        {
            Id = entity.Id,
            EmpresaId = entity.EmpresaId,
            DataCaixa = entity.DataCaixa,
            ValorAbertura = entity.ValorAbertura,
            ValorFechamentoSistema = entity.ValorFechamentoSistema,
            ValorFechamentoInformado = entity.ValorFechamentoInformado,
            Diferenca = entity.Diferenca,
            Status = entity.Status,
            AbertoPor = entity.AbertoPor,
            FechadoPor = entity.FechadoPor,
            Ativo = entity.Ativo,
            DataAbertura = entity.DataAbertura,
            DataFechamento = entity.DataFechamento,
            Observacoes = entity.Observacoes
        };
    }
}