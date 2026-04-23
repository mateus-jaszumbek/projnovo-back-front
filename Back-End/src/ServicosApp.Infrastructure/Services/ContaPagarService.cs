using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class ContaPagarService : IContaPagarService
{
    private readonly AppDbContext _context;

    public ContaPagarService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ContaPagarDto> CriarAsync(Guid empresaId, CreateContaPagarDto dto, CancellationToken cancellationToken = default)
    {
        var fornecedor = await ObterFornecedorAsync(empresaId, dto.FornecedorId, cancellationToken);
        var entity = new ContaPagar
        {
            EmpresaId = empresaId,
            Descricao = dto.Descricao.Trim(),
            FornecedorId = fornecedor?.Id,
            Fornecedor = string.IsNullOrWhiteSpace(dto.Fornecedor) ? fornecedor?.Nome : dto.Fornecedor.Trim(),
            Categoria = string.IsNullOrWhiteSpace(dto.Categoria) ? null : dto.Categoria.Trim(),
            DataEmissao = dto.DataEmissao ?? DateOnly.FromDateTime(DateTime.UtcNow),
            DataVencimento = dto.DataVencimento,
            Valor = dto.Valor,
            ValorPago = 0,
            Status = "PENDENTE",
            Observacoes = string.IsNullOrWhiteSpace(dto.Observacoes) ? null : dto.Observacoes.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.ContasPagar.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return await ObterPorIdAsync(empresaId, entity.Id, cancellationToken)
            ?? throw new InvalidOperationException("Erro ao carregar a conta a pagar criada.");
    }

    public async Task<List<ContaPagarDto>> ListarAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        return await _context.ContasPagar
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .OrderBy(x => x.DataVencimento)
            .Select(x => new ContaPagarDto
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                Descricao = x.Descricao,
                FornecedorId = x.FornecedorId,
                Fornecedor = x.Fornecedor ?? (x.FornecedorCadastro == null ? null : x.FornecedorCadastro.Nome),
                Categoria = x.Categoria,
                DataEmissao = x.DataEmissao,
                DataVencimento = x.DataVencimento,
                Valor = x.Valor,
                ValorPago = x.ValorPago,
                Status = x.Status,
                Observacoes = x.Observacoes,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ContaPagarDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ContasPagar
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Id == id)
            .Select(x => new ContaPagarDto
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                Descricao = x.Descricao,
                FornecedorId = x.FornecedorId,
                Fornecedor = x.Fornecedor ?? (x.FornecedorCadastro == null ? null : x.FornecedorCadastro.Nome),
                Categoria = x.Categoria,
                DataEmissao = x.DataEmissao,
                DataVencimento = x.DataVencimento,
                Valor = x.Valor,
                ValorPago = x.ValorPago,
                Status = x.Status,
                Observacoes = x.Observacoes,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Fornecedor?> ObterFornecedorAsync(Guid empresaId, Guid? fornecedorId, CancellationToken cancellationToken)
    {
        if (!fornecedorId.HasValue)
            return null;

        var fornecedor = await _context.Fornecedores
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == fornecedorId.Value && x.Ativo, cancellationToken);

        if (fornecedor is null)
            throw new InvalidOperationException("Fornecedor nao encontrado.");

        return fornecedor;
    }

    public async Task<ContaPagarDto?> PagarAsync(Guid empresaId, Guid? usuarioId, Guid id, PagarContaPagarDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ContasPagar
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return null;

        if (entity.Status == "PAGO")
            throw new InvalidOperationException("Esta conta já está totalmente paga.");

        if (dto.ValorPago <= 0)
            throw new InvalidOperationException("Valor pago deve ser maior que zero.");

        if (entity.ValorPago + dto.ValorPago > entity.Valor)
            throw new InvalidOperationException("Valor pago não pode ultrapassar o saldo da conta.");

        entity.ValorPago += dto.ValorPago;

        entity.Observacoes = string.IsNullOrWhiteSpace(dto.Observacoes)
            ? entity.Observacoes
            : dto.Observacoes.Trim();

        if (entity.ValorPago >= entity.Valor)
        {
            entity.ValorPago = entity.Valor;
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
                throw new InvalidOperationException("O caixa precisa estar aberto para pagar valores.");

            caixa.ValorFechamentoSistema -= dto.ValorPago;

            _context.CaixaLancamentos.Add(new CaixaLancamento
            {
                Id = Guid.NewGuid(),
                EmpresaId = empresaId,
                CaixaDiarioId = caixa.Id,
                Tipo = "SAIDA",
                OrigemTipo = "CONTA_PAGAR",
                OrigemId = entity.Id,
                FormaPagamento = null,
                Valor = dto.ValorPago,
                Observacao = $"Pagamento: {entity.Descricao}",
                CreatedBy = usuarioId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await ObterPorIdAsync(empresaId, entity.Id, cancellationToken);
    }
}
