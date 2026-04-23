using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class EstoqueMovimentoService : IEstoqueMovimentoService
{
    private readonly AppDbContext _context;

    public EstoqueMovimentoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EstoqueMovimentoDto> RegistrarEntradaAsync(
        Guid empresaId,
        CreateEstoqueEntradaDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.Quantidade <= 0)
            throw new InvalidOperationException("Quantidade deve ser maior que zero.");

        var peca = await _context.Pecas
            .FirstOrDefaultAsync(
                x => x.EmpresaId == empresaId && x.Id == dto.PecaId && x.Ativo,
                cancellationToken);

        if (peca is null)
            throw new InvalidOperationException("Peça não encontrada.");

        peca.EstoqueAtual += dto.Quantidade;

        if (dto.CustoUnitario.HasValue)
            peca.CustoUnitario = dto.CustoUnitario.Value;

        var movimento = new EstoqueMovimento
        {
            EmpresaId = empresaId,
            PecaId = dto.PecaId,
            TipoMovimento = "ENTRADA",
            OrigemTipo = "MANUAL",
            OrigemId = null,
            Quantidade = dto.Quantidade,
            CustoUnitario = dto.CustoUnitario ?? peca.CustoUnitario,
            Observacao = Normalizar(dto.Observacao),
            DataMovimento = DateTime.UtcNow
        };

        _context.EstoqueMovimentos.Add(movimento);
        await _context.SaveChangesAsync(cancellationToken);

        return Map(movimento);
    }

    public async Task<EstoqueMovimentoDto> RegistrarSaidaAsync(
        Guid empresaId,
        CreateEstoqueSaidaDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.Quantidade <= 0)
            throw new InvalidOperationException("Quantidade deve ser maior que zero.");

        var peca = await _context.Pecas
            .FirstOrDefaultAsync(
                x => x.EmpresaId == empresaId && x.Id == dto.PecaId && x.Ativo,
                cancellationToken);

        if (peca is null)
            throw new InvalidOperationException("Peça não encontrada.");

        if (peca.EstoqueAtual < dto.Quantidade)
            throw new InvalidOperationException("Estoque insuficiente.");

        peca.EstoqueAtual -= dto.Quantidade;

        var movimento = new EstoqueMovimento
        {
            EmpresaId = empresaId,
            PecaId = dto.PecaId,
            TipoMovimento = "SAIDA",
            OrigemTipo = "MANUAL",
            OrigemId = null,
            Quantidade = dto.Quantidade,
            CustoUnitario = peca.CustoUnitario,
            Observacao = Normalizar(dto.Observacao),
            DataMovimento = DateTime.UtcNow
        };

        _context.EstoqueMovimentos.Add(movimento);
        await _context.SaveChangesAsync(cancellationToken);

        return Map(movimento);
    }

    public async Task<EstoqueMovimentoDto> RegistrarConsumoOrdemServicoAsync(
        Guid empresaId,
        CreateConsumoOrdemServicoDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.Quantidade <= 0)
            throw new InvalidOperationException("Quantidade deve ser maior que zero.");

        var ordemServicoExiste = await _context.OrdensServico
            .AsNoTracking()
            .AnyAsync(x => x.EmpresaId == empresaId && x.Id == dto.OrdemServicoId, cancellationToken);

        if (!ordemServicoExiste)
            throw new InvalidOperationException("OS não encontrada.");

        var peca = await _context.Pecas
            .FirstOrDefaultAsync(
                x => x.EmpresaId == empresaId && x.Id == dto.PecaId && x.Ativo,
                cancellationToken);

        if (peca is null)
            throw new InvalidOperationException("Peça não encontrada.");

        if (peca.EstoqueAtual < dto.Quantidade)
            throw new InvalidOperationException("Estoque insuficiente.");

        peca.EstoqueAtual -= dto.Quantidade;

        var movimento = new EstoqueMovimento
        {
            EmpresaId = empresaId,
            PecaId = dto.PecaId,
            TipoMovimento = "CONSUMO_OS",
            OrigemTipo = "ORDEM_SERVICO",
            OrigemId = dto.OrdemServicoId,
            Quantidade = dto.Quantidade,
            CustoUnitario = peca.CustoUnitario,
            Observacao = Normalizar(dto.Observacao),
            DataMovimento = DateTime.UtcNow
        };

        _context.EstoqueMovimentos.Add(movimento);
        await _context.SaveChangesAsync(cancellationToken);

        return Map(movimento);
    }

    public async Task<List<EstoqueMovimentoDto>> ListarPorPecaAsync(
        Guid empresaId,
        Guid pecaId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EstoqueMovimentos
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.PecaId == pecaId)
            .OrderByDescending(x => x.DataMovimento)
            .Select(x => new EstoqueMovimentoDto
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                PecaId = x.PecaId,
                TipoMovimento = x.TipoMovimento,
                OrigemTipo = x.OrigemTipo,
                OrigemId = x.OrigemId,
                Quantidade = x.Quantidade,
                CustoUnitario = x.CustoUnitario,
                Observacao = x.Observacao,
                DataMovimento = x.DataMovimento,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    private static string? Normalizar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static EstoqueMovimentoDto Map(EstoqueMovimento entity)
    {
        return new EstoqueMovimentoDto
        {
            Id = entity.Id,
            EmpresaId = entity.EmpresaId,
            PecaId = entity.PecaId,
            TipoMovimento = entity.TipoMovimento,
            OrigemTipo = entity.OrigemTipo,
            OrigemId = entity.OrigemId,
            Quantidade = entity.Quantidade,
            CustoUnitario = entity.CustoUnitario,
            Observacao = entity.Observacao,
            DataMovimento = entity.DataMovimento,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}