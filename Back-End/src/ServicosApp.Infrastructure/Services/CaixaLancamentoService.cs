using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class CaixaLancamentoService : ICaixaLancamentoService
{
    private readonly AppDbContext _context;

    public CaixaLancamentoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CaixaLancamentoDto> LancarAsync(Guid empresaId, Guid? usuarioId, CreateCaixaLancamentoDto dto, CancellationToken cancellationToken = default)
    {
        var caixa = await _context.CaixasDiarios
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == dto.CaixaDiarioId && x.Ativo, cancellationToken);

        if (caixa is null)
            throw new InvalidOperationException("Caixa não encontrado.");

        if (caixa.Status != "ABERTO")
            throw new InvalidOperationException("O caixa precisa estar aberto para receber lançamentos.");

        var tipo = dto.Tipo.Trim().ToUpperInvariant();

        if (tipo != "ENTRADA" && tipo != "SAIDA")
            throw new InvalidOperationException("Tipo deve ser ENTRADA ou SAIDA.");

        var lancamento = new CaixaLancamento
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            CaixaDiarioId = dto.CaixaDiarioId,
            Tipo = tipo,
            OrigemTipo = string.IsNullOrWhiteSpace(dto.OrigemTipo) ? null : dto.OrigemTipo.Trim().ToUpperInvariant(),
            OrigemId = dto.OrigemId,
            FormaPagamento = string.IsNullOrWhiteSpace(dto.FormaPagamento) ? null : dto.FormaPagamento.Trim().ToUpperInvariant(),
            Valor = dto.Valor,
            Observacao = string.IsNullOrWhiteSpace(dto.Observacao) ? null : dto.Observacao.Trim(),
            CreatedBy = usuarioId,
            CreatedAt = DateTime.UtcNow
        };

        if (tipo == "ENTRADA")
            caixa.ValorFechamentoSistema += dto.Valor;
        else
            caixa.ValorFechamentoSistema -= dto.Valor;

        _context.CaixaLancamentos.Add(lancamento);
        await _context.SaveChangesAsync(cancellationToken);

        return Map(lancamento);
    }

    public async Task<List<CaixaLancamentoDto>> ListarPorCaixaAsync(Guid empresaId, Guid caixaDiarioId, CancellationToken cancellationToken = default)
    {
        return await _context.CaixaLancamentos
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.CaixaDiarioId == caixaDiarioId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CaixaLancamentoDto
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                CaixaDiarioId = x.CaixaDiarioId,
                Tipo = x.Tipo,
                OrigemTipo = x.OrigemTipo,
                OrigemId = x.OrigemId,
                FormaPagamento = x.FormaPagamento,
                Valor = x.Valor,
                Observacao = x.Observacao,
                CreatedBy = x.CreatedBy,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    private static CaixaLancamentoDto Map(CaixaLancamento entity)
    {
        return new CaixaLancamentoDto
        {
            Id = entity.Id,
            EmpresaId = entity.EmpresaId,
            CaixaDiarioId = entity.CaixaDiarioId,
            Tipo = entity.Tipo,
            OrigemTipo = entity.OrigemTipo,
            OrigemId = entity.OrigemId,
            FormaPagamento = entity.FormaPagamento,
            Valor = entity.Valor,
            Observacao = entity.Observacao,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt
        };
    }
}
