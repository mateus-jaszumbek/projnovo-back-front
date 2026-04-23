using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class ContaReceberService : IContaReceberService
{
    private readonly AppDbContext _context;

    public ContaReceberService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ContaReceberDto> CriarAsync(Guid empresaId, CreateContaReceberDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.ClienteId.HasValue)
        {
            var clienteExiste = await _context.Clientes
                .AsNoTracking()
                .AnyAsync(x => x.EmpresaId == empresaId && x.Id == dto.ClienteId.Value, cancellationToken);

            if (!clienteExiste)
                throw new InvalidOperationException("Cliente não encontrado.");
        }

        var entity = new ContaReceber
        {
            EmpresaId = empresaId,
            ClienteId = dto.ClienteId,
            OrigemTipo = string.IsNullOrWhiteSpace(dto.OrigemTipo) ? null : dto.OrigemTipo.Trim().ToUpperInvariant(),
            OrigemId = dto.OrigemId,
            Descricao = dto.Descricao.Trim(),
            DataEmissao = dto.DataEmissao ?? DateOnly.FromDateTime(DateTime.UtcNow),
            DataVencimento = dto.DataVencimento,
            Valor = dto.Valor,
            ValorRecebido = 0,
            Status = "PENDENTE",
            FormaPagamento = string.IsNullOrWhiteSpace(dto.FormaPagamento) ? null : dto.FormaPagamento.Trim().ToUpperInvariant(),
            Observacoes = string.IsNullOrWhiteSpace(dto.Observacoes) ? null : dto.Observacoes.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.ContasReceber.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return await ObterPorIdAsync(empresaId, entity.Id, cancellationToken)
            ?? throw new InvalidOperationException("Erro ao carregar a conta a receber criada.");
    }

    public async Task<List<ContaReceberDto>> ListarAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        return await _context.ContasReceber
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .OrderBy(x => x.DataVencimento)
            .Select(x => new ContaReceberDto
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                ClienteId = x.ClienteId,
                ClienteNome = x.Cliente != null ? x.Cliente.Nome : null,
                OrigemTipo = x.OrigemTipo,
                OrigemId = x.OrigemId,
                Descricao = x.Descricao,
                DataEmissao = x.DataEmissao,
                DataVencimento = x.DataVencimento,
                Valor = x.Valor,
                ValorRecebido = x.ValorRecebido,
                Status = x.Status,
                FormaPagamento = x.FormaPagamento,
                Observacoes = x.Observacoes,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ContaReceberDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ContasReceber
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Id == id)
            .Select(x => new ContaReceberDto
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                ClienteId = x.ClienteId,
                ClienteNome = x.Cliente != null ? x.Cliente.Nome : null,
                OrigemTipo = x.OrigemTipo,
                OrigemId = x.OrigemId,
                Descricao = x.Descricao,
                DataEmissao = x.DataEmissao,
                DataVencimento = x.DataVencimento,
                Valor = x.Valor,
                ValorRecebido = x.ValorRecebido,
                Status = x.Status,
                FormaPagamento = x.FormaPagamento,
                Observacoes = x.Observacoes,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ContaReceberDto?> ReceberAsync(Guid empresaId, Guid? usuarioId, Guid id, ReceberContaReceberDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ContasReceber
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return null;

        if (entity.Status == "PAGO")
            throw new InvalidOperationException("Esta conta já está totalmente recebida.");

        if (dto.ValorRecebido <= 0)
            throw new InvalidOperationException("Valor recebido deve ser maior que zero.");

        if (entity.ValorRecebido + dto.ValorRecebido > entity.Valor)
            throw new InvalidOperationException("Valor recebido não pode ultrapassar o saldo da conta.");

        entity.ValorRecebido += dto.ValorRecebido;
        entity.FormaPagamento = string.IsNullOrWhiteSpace(dto.FormaPagamento)
            ? entity.FormaPagamento
            : dto.FormaPagamento.Trim().ToUpperInvariant();

        entity.Observacoes = string.IsNullOrWhiteSpace(dto.Observacoes)
            ? entity.Observacoes
            : dto.Observacoes.Trim();

        if (entity.ValorRecebido >= entity.Valor)
        {
            entity.ValorRecebido = entity.Valor;
            entity.Status = "PAGO";
        }
        else
        {
            entity.Status = "PARCIAL";
        }

        if (dto.CaixaDiarioId.HasValue)
        {
            var caixa = await _context.CaixasDiarios
                .FirstOrDefaultAsync(x =>
                    x.EmpresaId == empresaId &&
                    x.Id == dto.CaixaDiarioId.Value &&
                    x.Ativo,
                    cancellationToken);

            if (caixa is null)
                throw new InvalidOperationException("Caixa não encontrado.");

            if (caixa.Status != "ABERTO")
                throw new InvalidOperationException("O caixa precisa estar aberto para receber valores.");

            caixa.ValorFechamentoSistema += dto.ValorRecebido;

            _context.CaixaLancamentos.Add(new CaixaLancamento
            {
                Id = Guid.NewGuid(),
                EmpresaId = empresaId,
                CaixaDiarioId = caixa.Id,
                Tipo = "ENTRADA",
                OrigemTipo = "CONTA_RECEBER",
                OrigemId = entity.Id,
                FormaPagamento = entity.FormaPagamento,
                Valor = dto.ValorRecebido,
                Observacao = $"Recebimento: {entity.Descricao}",
                CreatedBy = usuarioId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await ObterPorIdAsync(empresaId, entity.Id, cancellationToken);
    }
}
