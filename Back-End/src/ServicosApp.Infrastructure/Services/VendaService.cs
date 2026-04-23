using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class VendaService : IVendaService
{
    private readonly AppDbContext _context;

    public VendaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<VendaDto> CriarAsync(Guid empresaId, Guid? usuarioId, CreateVendaDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Desconto < 0)
            throw new InvalidOperationException("Desconto não pode ser negativo.");

        if (dto.ClienteId.HasValue)
        {
            var clienteExiste = await _context.Clientes
                .AsNoTracking()
                .AnyAsync(x => x.EmpresaId == empresaId && x.Id == dto.ClienteId.Value, cancellationToken);

            if (!clienteExiste)
                throw new InvalidOperationException("Cliente não encontrado para esta empresa.");
        }

        var ultimoNumero = await _context.Vendas
            .Where(x => x.EmpresaId == empresaId)
            .Select(x => (long?)x.NumeroVenda)
            .MaxAsync(cancellationToken);

        var entity = new Venda
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            NumeroVenda = (ultimoNumero ?? 0) + 1,
            ClienteId = dto.ClienteId,
            Status = "ABERTA",
            FormaPagamento = string.IsNullOrWhiteSpace(dto.FormaPagamento) ? "DINHEIRO" : dto.FormaPagamento.Trim().ToUpperInvariant(),
            Subtotal = 0,
            Desconto = dto.Desconto,
            ValorTotal = 0,
            Observacoes = string.IsNullOrWhiteSpace(dto.Observacoes) ? null : dto.Observacoes.Trim(),
            DataVenda = DateTime.UtcNow,
            Ativo = true,
            CreatedBy = usuarioId
        };

        _context.Vendas.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return await ObterPorIdAsync(empresaId, entity.Id, cancellationToken)
            ?? throw new InvalidOperationException("Erro ao carregar a venda criada.");
    }

    public async Task<VendaDto> CriarComItensAsync(Guid empresaId, Guid? usuarioId, CreateVendaComItensDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Itens.Count == 0)
            throw new InvalidOperationException("Adicione pelo menos um item na venda.");

        if (dto.Desconto < 0)
            throw new InvalidOperationException("Desconto não pode ser negativo.");

        if (dto.ClienteId.HasValue)
        {
            var clienteExiste = await _context.Clientes
                .AsNoTracking()
                .AnyAsync(x => x.EmpresaId == empresaId && x.Id == dto.ClienteId.Value, cancellationToken);

            if (!clienteExiste)
                throw new InvalidOperationException("Cliente não encontrado para esta empresa.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var ultimoNumero = await _context.Vendas
            .Where(x => x.EmpresaId == empresaId)
            .Select(x => (long?)x.NumeroVenda)
            .MaxAsync(cancellationToken);

        var venda = new Venda
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            NumeroVenda = (ultimoNumero ?? 0) + 1,
            ClienteId = dto.ClienteId,
            Status = dto.Finalizar ? "FECHADA" : "ABERTA",
            FormaPagamento = string.IsNullOrWhiteSpace(dto.FormaPagamento) ? "DINHEIRO" : dto.FormaPagamento.Trim().ToUpperInvariant(),
            Desconto = dto.Desconto,
            Observacoes = string.IsNullOrWhiteSpace(dto.Observacoes) ? null : dto.Observacoes.Trim(),
            DataVenda = DateTime.UtcNow,
            Ativo = true,
            CreatedBy = usuarioId
        };

        _context.Vendas.Add(venda);

        foreach (var itemDto in dto.Itens)
        {
            if (itemDto.Quantidade <= 0)
                throw new InvalidOperationException("Quantidade deve ser maior que zero.");

            if (itemDto.Desconto < 0)
                throw new InvalidOperationException("Desconto do item não pode ser negativo.");

            var peca = await _context.Pecas
                .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == itemDto.PecaId && x.Ativo, cancellationToken);

            if (peca is null)
                throw new InvalidOperationException("Peça não encontrada.");

            if (peca.EstoqueAtual < itemDto.Quantidade)
                throw new InvalidOperationException($"Estoque insuficiente para {peca.Nome}.");

            var valorUnitario = itemDto.ValorUnitario ?? peca.PrecoVenda;

            if (valorUnitario < 0)
                throw new InvalidOperationException("Valor unitário não pode ser negativo.");

            peca.EstoqueAtual -= itemDto.Quantidade;

            var item = new VendaItem
            {
                EmpresaId = empresaId,
                VendaId = venda.Id,
                PecaId = peca.Id,
                Descricao = peca.Nome,
                Quantidade = itemDto.Quantidade,
                CustoUnitario = peca.CustoUnitario,
                ValorUnitario = valorUnitario,
                Desconto = itemDto.Desconto,
                ValorTotal = (itemDto.Quantidade * valorUnitario) - itemDto.Desconto
            };

            if (item.ValorTotal < 0)
                item.ValorTotal = 0;

            venda.Itens.Add(item);
            _context.VendaItens.Add(item);

            _context.EstoqueMovimentos.Add(new EstoqueMovimento
            {
                EmpresaId = empresaId,
                PecaId = peca.Id,
                TipoMovimento = "VENDA",
                OrigemTipo = "VENDA",
                OrigemId = venda.Id,
                Quantidade = itemDto.Quantidade,
                CustoUnitario = peca.CustoUnitario,
                Observacao = $"Saída por venda #{venda.NumeroVenda}",
                DataMovimento = DateTime.UtcNow
            });
        }

        venda.Subtotal = venda.Itens.Sum(x => x.ValorTotal);
        venda.ValorTotal = venda.Subtotal - venda.Desconto;
        if (venda.ValorTotal < 0)
            venda.ValorTotal = 0;

        if (venda.Status == "FECHADA")
            await RegistrarFinanceiroVendaAsync(empresaId, usuarioId, venda, dto.Parcelas, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await ObterPorIdAsync(empresaId, venda.Id, cancellationToken)
            ?? throw new InvalidOperationException("Erro ao carregar a venda criada.");
    }

    public async Task<List<VendaDto>> ListarAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        return await _context.Vendas
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .OrderByDescending(x => x.NumeroVenda)
            .Select(x => new VendaDto
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                NumeroVenda = x.NumeroVenda,
                ClienteId = x.ClienteId,
                ClienteNome = x.Cliente != null ? x.Cliente.Nome : null,
                Status = x.Status,
                FormaPagamento = x.FormaPagamento,
                Subtotal = x.Subtotal,
                Desconto = x.Desconto,
                ValorTotal = x.ValorTotal,
                Observacoes = x.Observacoes,
                DataVenda = x.DataVenda,
                Ativo = x.Ativo,
                CreatedBy = x.CreatedBy,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<VendaDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Vendas
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Id == id)
            .Select(x => new VendaDto
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                NumeroVenda = x.NumeroVenda,
                ClienteId = x.ClienteId,
                ClienteNome = x.Cliente != null ? x.Cliente.Nome : null,
                Status = x.Status,
                FormaPagamento = x.FormaPagamento,
                Subtotal = x.Subtotal,
                Desconto = x.Desconto,
                ValorTotal = x.ValorTotal,
                Observacoes = x.Observacoes,
                DataVenda = x.DataVenda,
                Ativo = x.Ativo,
                CreatedBy = x.CreatedBy,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<VendaDto?> AtualizarAsync(Guid empresaId, Guid id, UpdateVendaDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Desconto < 0)
            throw new InvalidOperationException("Desconto não pode ser negativo.");

        var entity = await _context.Vendas
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return null;

        if (entity.Status == "CANCELADA")
            throw new InvalidOperationException("Não é possível alterar uma venda cancelada.");

        if (dto.ClienteId.HasValue)
        {
            var clienteExiste = await _context.Clientes
                .AsNoTracking()
                .AnyAsync(x => x.EmpresaId == empresaId && x.Id == dto.ClienteId.Value, cancellationToken);

            if (!clienteExiste)
                throw new InvalidOperationException("Cliente não encontrado para esta empresa.");
        }

        entity.ClienteId = dto.ClienteId;
        entity.FormaPagamento = string.IsNullOrWhiteSpace(dto.FormaPagamento) ? "DINHEIRO" : dto.FormaPagamento.Trim().ToUpperInvariant();
        entity.Desconto = dto.Desconto;
        entity.Observacoes = string.IsNullOrWhiteSpace(dto.Observacoes) ? null : dto.Observacoes.Trim();
        entity.Ativo = dto.Ativo;

        entity.Subtotal = entity.Itens.Sum(x => x.ValorTotal);
        entity.ValorTotal = entity.Subtotal - entity.Desconto;
        if (entity.ValorTotal < 0)
            entity.ValorTotal = 0;

        await _context.SaveChangesAsync(cancellationToken);

        return await ObterPorIdAsync(empresaId, entity.Id, cancellationToken);
    }

    public async Task<bool> FinalizarAsync(Guid empresaId, Guid? usuarioId, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Vendas
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return false;

        if (entity.Status == "CANCELADA")
            throw new InvalidOperationException("Não é possível finalizar uma venda cancelada.");

        entity.Subtotal = entity.Itens.Sum(x => x.ValorTotal);
        entity.ValorTotal = entity.Subtotal - entity.Desconto;
        if (entity.ValorTotal < 0)
            entity.ValorTotal = 0;

        if (entity.Status != "FECHADA")
        {
            entity.Status = "FECHADA";
            await RegistrarFinanceiroVendaAsync(empresaId, usuarioId, entity, null, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task RegistrarFinanceiroVendaAsync(
        Guid empresaId,
        Guid? usuarioId,
        Venda venda,
        List<VendaParcelaDto>? parcelas,
        CancellationToken cancellationToken)
    {
        if (venda.ValorTotal <= 0)
            return;

        var jaLancouCaixa = await _context.CaixaLancamentos
            .AnyAsync(x => x.EmpresaId == empresaId && x.OrigemTipo == "VENDA" && x.OrigemId == venda.Id, cancellationToken);

        var jaGerouReceber = await _context.ContasReceber
            .AnyAsync(x => x.EmpresaId == empresaId && x.OrigemTipo == "VENDA" && x.OrigemId == venda.Id, cancellationToken);

        if (jaLancouCaixa || jaGerouReceber)
            return;

        var formaPagamento = venda.FormaPagamento.Trim().ToUpperInvariant();
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        if (parcelas is { Count: > 0 })
        {
            var totalParcelas = parcelas.Sum(x => x.Valor);
            if (Math.Abs(totalParcelas - venda.ValorTotal) > 0.01m)
                throw new InvalidOperationException("A soma das parcelas deve ser igual ao total da venda.");

            for (var index = 0; index < parcelas.Count; index++)
            {
                var parcela = parcelas[index];
                var taxaValor = Math.Round(parcela.Valor * parcela.TaxaPercentual / 100, 2);
                var liquido = parcela.Valor - taxaValor;
                var formaParcela = string.IsNullOrWhiteSpace(parcela.FormaPagamento)
                    ? formaPagamento
                    : parcela.FormaPagamento.Trim().ToUpperInvariant();

                _context.ContasReceber.Add(new ContaReceber
                {
                    Id = Guid.NewGuid(),
                    EmpresaId = empresaId,
                    ClienteId = venda.ClienteId,
                    OrigemTipo = "VENDA",
                    OrigemId = venda.Id,
                    Descricao = $"Venda #{venda.NumeroVenda} - parcela {index + 1}/{parcelas.Count}",
                    DataEmissao = hoje,
                    DataVencimento = parcela.DataVencimento,
                    Valor = parcela.Valor,
                    ValorRecebido = 0,
                    Status = "PENDENTE",
                    FormaPagamento = formaParcela,
                    Observacoes = $"Taxa prevista: {parcela.TaxaPercentual:N2}% ({taxaValor:N2}). Líquido previsto: {liquido:N2}.",
                    CreatedAt = DateTime.UtcNow
                });
            }

            return;
        }

        if (formaPagamento == "BOLETO" || formaPagamento == "CARTAO_CREDITO" || formaPagamento == "CREDIARIO")
        {
            _context.ContasReceber.Add(new ContaReceber
            {
                Id = Guid.NewGuid(),
                EmpresaId = empresaId,
                ClienteId = venda.ClienteId,
                OrigemTipo = "VENDA",
                OrigemId = venda.Id,
                Descricao = $"Venda #{venda.NumeroVenda}",
                DataEmissao = hoje,
                DataVencimento = hoje,
                Valor = venda.ValorTotal,
                ValorRecebido = 0,
                Status = "PENDENTE",
                FormaPagamento = formaPagamento,
                Observacoes = "Gerado automaticamente ao finalizar venda.",
                CreatedAt = DateTime.UtcNow
            });
            return;
        }

        var caixa = await _context.CaixasDiarios
            .FirstOrDefaultAsync(x =>
                x.EmpresaId == empresaId &&
                x.DataCaixa == hoje &&
                x.Status == "ABERTO" &&
                x.Ativo,
                cancellationToken);

        if (caixa is null)
            throw new InvalidOperationException("Abra o caixa do dia antes de finalizar vendas à vista.");

        caixa.ValorFechamentoSistema += venda.ValorTotal;

        _context.CaixaLancamentos.Add(new CaixaLancamento
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            CaixaDiarioId = caixa.Id,
            Tipo = "ENTRADA",
            OrigemTipo = "VENDA",
            OrigemId = venda.Id,
            FormaPagamento = formaPagamento,
            Valor = venda.ValorTotal,
            Observacao = $"Venda #{venda.NumeroVenda}",
            CreatedBy = usuarioId,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task<bool> CancelarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Vendas
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return false;

        if (entity.Status == "CANCELADA")
            return true;

        foreach (var item in entity.Itens)
        {
            var peca = await _context.Pecas
                .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == item.PecaId, cancellationToken);

            if (peca != null)
            {
                peca.EstoqueAtual += item.Quantidade;

                _context.EstoqueMovimentos.Add(new EstoqueMovimento
                {
                    EmpresaId = empresaId,
                    PecaId = peca.Id,
                    TipoMovimento = "ESTORNO_VENDA",
                    OrigemTipo = "VENDA",
                    OrigemId = entity.Id,
                    Quantidade = item.Quantidade,
                    CustoUnitario = peca.CustoUnitario,
                    Observacao = $"Estorno da venda #{entity.NumeroVenda}",
                    DataMovimento = DateTime.UtcNow
                });
            }
        }

        entity.Status = "CANCELADA";
        entity.Ativo = false;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
