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

        var peca = await _context.Pecas
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == dto.PecaId && x.Ativo, cancellationToken);

        if (peca is null)
            throw new InvalidOperationException("Peça não encontrada.");

        if (dto.Quantidade <= 0)
            throw new InvalidOperationException("Quantidade deve ser maior que zero.");

        if (dto.Desconto < 0)
            throw new InvalidOperationException("Desconto não pode ser negativo.");

        if (peca.EstoqueAtual < dto.Quantidade)
            throw new InvalidOperationException("Estoque insuficiente.");

        var valorUnitario = dto.ValorUnitario ?? peca.PrecoVenda;

        if (valorUnitario < 0)
            throw new InvalidOperationException("Valor unitário não pode ser negativo.");

        peca.EstoqueAtual -= dto.Quantidade;

        var item = new VendaItem
        {
            EmpresaId = empresaId,
            VendaId = vendaId,
            PecaId = peca.Id,
            Descricao = peca.Nome,
            Quantidade = dto.Quantidade,
            CustoUnitario = peca.CustoUnitario,
            ValorUnitario = valorUnitario,
            Desconto = dto.Desconto,
            ValorTotal = (dto.Quantidade * valorUnitario) - dto.Desconto
        };

        if (item.ValorTotal < 0)
            item.ValorTotal = 0;

        _context.VendaItens.Add(item);
        venda.Itens.Add(item);

        venda.Subtotal = venda.Itens.Sum(x => x.ValorTotal);
        venda.ValorTotal = venda.Subtotal - venda.Desconto;
        if (venda.ValorTotal < 0)
            venda.ValorTotal = 0;

        _context.EstoqueMovimentos.Add(new EstoqueMovimento
        {
            EmpresaId = empresaId,
            PecaId = peca.Id,
            TipoMovimento = "VENDA",
            OrigemTipo = "VENDA",
            OrigemId = venda.Id,
            Quantidade = dto.Quantidade,
            CustoUnitario = peca.CustoUnitario,
            Observacao = $"Saída por venda #{venda.NumeroVenda}",
            DataMovimento = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

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
                PecaId = x.PecaId,
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
                OrigemId = venda.Id,
                Quantidade = item.Quantidade,
                CustoUnitario = peca.CustoUnitario,
                Observacao = $"Remoção de item da venda #{venda.NumeroVenda}",
                DataMovimento = DateTime.UtcNow
            });
        }

        _context.VendaItens.Remove(item);
        venda.Itens.Remove(item);

        venda.Subtotal = venda.Itens.Sum(x => x.ValorTotal);
        venda.ValorTotal = venda.Subtotal - venda.Desconto;
        if (venda.ValorTotal < 0)
            venda.ValorTotal = 0;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static VendaItemDto Map(VendaItem item)
    {
        return new VendaItemDto
        {
            Id = item.Id,
            EmpresaId = item.EmpresaId,
            VendaId = item.VendaId,
            PecaId = item.PecaId,
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