using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class VendaItemService : IVendaItemService
{
    private readonly AppDbContext _context;

    public VendaItemService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<VendaItemDto> AdicionarAsync(Guid empresaId, Guid vendaId, CreateVendaItemDto dto, CancellationToken cancellationToken = default)
    {
        var venda = await _context.Vendas
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == vendaId, cancellationToken);

        if (venda is null)
            throw new InvalidOperationException("Venda não encontrada.");

        if (venda.Status == "CANCELADA" || venda.Status == "FECHADA")
            throw new InvalidOperationException("Não é possível alterar itens de uma venda cancelada ou fechada.");

        var item = new VendaItem
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            VendaId = vendaId
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await PreencherItemAsync(
            empresaId,
            item,
            dto.TipoItem,
            dto.ServicoCatalogoId,
            dto.PecaId,
            dto.Descricao,
            dto.Quantidade,
            dto.ValorUnitario,
            dto.Desconto,
            cancellationToken);

        if (item.TipoItem == "PECA" && item.PecaId.HasValue)
            await BaixarEstoqueAsync(empresaId, venda, item.PecaId.Value, item.Quantidade, cancellationToken);

        _context.VendaItens.Add(item);
        venda.Itens.Add(item);

        venda.Subtotal = venda.Itens.Sum(x => x.ValorTotal);
        venda.ValorTotal = venda.Subtotal - venda.Desconto;
        if (venda.ValorTotal < 0)
            venda.ValorTotal = 0;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Map(item);
    }

    public async Task<List<VendaItemDto>> ListarAsync(Guid empresaId, Guid vendaId, CancellationToken cancellationToken = default)
    {
        return await _context.VendaItens
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.VendaId == vendaId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new VendaItemDto
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                VendaId = x.VendaId,
                TipoItem = x.TipoItem,
                PecaId = x.PecaId,
                ServicoCatalogoId = x.ServicoCatalogoId,
                Descricao = x.Descricao,
                Quantidade = x.Quantidade,
                CustoUnitario = x.CustoUnitario,
                ValorUnitario = x.ValorUnitario,
                Desconto = x.Desconto,
                ValorTotal = x.ValorTotal,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RemoverAsync(Guid empresaId, Guid vendaId, Guid itemId, CancellationToken cancellationToken = default)
    {
        var venda = await _context.Vendas
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == vendaId, cancellationToken);

        if (venda is null)
            return false;

        if (venda.Status == "CANCELADA" || venda.Status == "FECHADA")
            throw new InvalidOperationException("Não é possível remover itens de uma venda cancelada ou fechada.");

        var item = venda.Itens.FirstOrDefault(x => x.Id == itemId);
        if (item is null)
            return false;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        if (item.TipoItem == "PECA" && item.PecaId.HasValue)
            await RestaurarEstoqueAsync(empresaId, venda, item.PecaId.Value, item.Quantidade, cancellationToken);

        _context.VendaItens.Remove(item);
        venda.Itens.Remove(item);

        venda.Subtotal = venda.Itens.Sum(x => x.ValorTotal);
        venda.ValorTotal = venda.Subtotal - venda.Desconto;
        if (venda.ValorTotal < 0)
            venda.ValorTotal = 0;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task PreencherItemAsync(
        Guid empresaId,
        VendaItem item,
        string tipoItem,
        Guid? servicoCatalogoId,
        Guid? pecaId,
        string? descricao,
        decimal quantidade,
        decimal? valorUnitario,
        decimal desconto,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tipoItem))
            throw new InvalidOperationException("Tipo do item é obrigatório.");

        if (quantidade <= 0)
            throw new InvalidOperationException("Quantidade deve ser maior que zero.");

        if (desconto < 0)
            throw new InvalidOperationException("Desconto não pode ser negativo.");

        var tipo = tipoItem.Trim().ToUpperInvariant();

        item.TipoItem = tipo;
        item.ServicoCatalogoId = null;
        item.PecaId = null;
        item.CustoUnitario = 0;

        switch (tipo)
        {
            case "SERVICO":
                {
                    if (servicoCatalogoId.HasValue)
                    {
                        var servico = await _context.ServicosCatalogo
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                x => x.EmpresaId == empresaId && x.Id == servicoCatalogoId.Value && x.Ativo,
                                cancellationToken);

                        if (servico is null)
                            throw new InvalidOperationException("Serviço não encontrado.");

                        item.ServicoCatalogoId = servico.Id;
                        item.Descricao = !string.IsNullOrWhiteSpace(descricao) ? descricao.Trim() : servico.Nome;
                        item.ValorUnitario = valorUnitario ?? servico.ValorPadrao;
                        item.CustoUnitario = 0;
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(descricao))
                            throw new InvalidOperationException("Descrição é obrigatória para item manual.");

                        item.Descricao = descricao.Trim();
                        item.ValorUnitario = valorUnitario ?? 0;
                        item.CustoUnitario = 0;
                    }

                    break;
                }

            case "PECA":
                {
                    if (!pecaId.HasValue)
                        throw new InvalidOperationException("PecaId é obrigatório para item do tipo peça.");

                    var peca = await _context.Pecas
                        .FirstOrDefaultAsync(
                            x => x.EmpresaId == empresaId && x.Id == pecaId.Value && x.Ativo,
                            cancellationToken);

                    if (peca is null)
                        throw new InvalidOperationException("Peça não encontrada.");

                    if (peca.EstoqueAtual < quantidade)
                        throw new InvalidOperationException($"Estoque insuficiente para a peça '{peca.Nome}'.");

                    item.PecaId = peca.Id;
                    item.Descricao = !string.IsNullOrWhiteSpace(descricao) ? descricao.Trim() : peca.Nome;
                    item.CustoUnitario = peca.CustoUnitario;
                    item.ValorUnitario = valorUnitario ?? peca.PrecoVenda;
                    break;
                }

            default:
                throw new InvalidOperationException("TipoItem inválido. Use SERVICO ou PECA.");
        }

        if (item.ValorUnitario < 0)
            throw new InvalidOperationException("Valor unitário não pode ser negativo.");

        item.Quantidade = quantidade;
        item.Desconto = desconto;
        item.ValorTotal = (item.Quantidade * item.ValorUnitario) - item.Desconto;

        if (item.ValorTotal < 0)
            item.ValorTotal = 0;
    }

    private async Task BaixarEstoqueAsync(
        Guid empresaId,
        Venda venda,
        Guid pecaId,
        decimal quantidade,
        CancellationToken cancellationToken)
    {
        var peca = await _context.Pecas
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == pecaId, cancellationToken);

        if (peca is null)
            throw new InvalidOperationException("Peça não encontrada.");

        peca.EstoqueAtual -= quantidade;

        _context.EstoqueMovimentos.Add(new EstoqueMovimento
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            PecaId = peca.Id,
            TipoMovimento = "VENDA",
            OrigemTipo = "VENDA",
            OrigemId = venda.Id,
            Quantidade = quantidade,
            CustoUnitario = peca.CustoUnitario,
            Observacao = $"Saída por venda #{venda.NumeroVenda}",
            DataMovimento = DateTime.UtcNow
        });
    }

    private async Task RestaurarEstoqueAsync(
        Guid empresaId,
        Venda venda,
        Guid pecaId,
        decimal quantidade,
        CancellationToken cancellationToken)
    {
        var peca = await _context.Pecas
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == pecaId, cancellationToken);

        if (peca is null)
            return;

        peca.EstoqueAtual += quantidade;

        _context.EstoqueMovimentos.Add(new EstoqueMovimento
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            PecaId = peca.Id,
            TipoMovimento = "ESTORNO_VENDA",
            OrigemTipo = "VENDA",
            OrigemId = venda.Id,
            Quantidade = quantidade,
            CustoUnitario = peca.CustoUnitario,
            Observacao = $"Remoção de item da venda #{venda.NumeroVenda}",
            DataMovimento = DateTime.UtcNow
        });
    }

    private static VendaItemDto Map(VendaItem item)
    {
        return new VendaItemDto
        {
            Id = item.Id,
            EmpresaId = item.EmpresaId,
            VendaId = item.VendaId,
            TipoItem = item.TipoItem,
            PecaId = item.PecaId,
            ServicoCatalogoId = item.ServicoCatalogoId,
            Descricao = item.Descricao,
            Quantidade = item.Quantidade,
            CustoUnitario = item.CustoUnitario,
            ValorUnitario = item.ValorUnitario,
            Desconto = item.Desconto,
            ValorTotal = item.ValorTotal,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
