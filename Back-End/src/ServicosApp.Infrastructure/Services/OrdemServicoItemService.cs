using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class OrdemServicoItemService : IOrdemServicoItemService
{
    private readonly AppDbContext _context;

    public OrdemServicoItemService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OrdemServicoItemDto> AdicionarAsync(
        Guid empresaId,
        Guid ordemServicoId,
        CreateOrdemServicoItemDto dto,
        CancellationToken cancellationToken = default)
    {
        var ordemServico = await _context.OrdensServico
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == ordemServicoId, cancellationToken);

        if (ordemServico is null)
            throw new InvalidOperationException("OS não encontrada.");

        if (ordemServico.Status == "CANCELADA" || ordemServico.Status == "ENTREGUE")
            throw new InvalidOperationException("Não é possível alterar itens de uma OS cancelada ou entregue.");

        var item = new OrdemServicoItem
        {
            EmpresaId = empresaId,
            OrdemServicoId = ordemServicoId,
            Ordem = ordemServico.Itens.Count == 0 ? 1 : ordemServico.Itens.Max(x => x.Ordem) + 1
        };

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

        _context.OrdensServicoItens.Add(item);
        ordemServico.Itens.Add(item);

        RecalcularTotais(ordemServico);

        await _context.SaveChangesAsync(cancellationToken);

        return Map(item);
    }

    public async Task<List<OrdemServicoItemDto>> ListarAsync(
     Guid empresaId,
     Guid ordemServicoId,
     CancellationToken cancellationToken = default)
    {
        var itens = await _context.OrdensServicoItens
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.OrdemServicoId == ordemServicoId)
            .Select(x => new OrdemServicoItemDto
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                OrdemServicoId = x.OrdemServicoId,
                TipoItem = x.TipoItem,
                Ordem = x.Ordem,
                ServicoCatalogoId = x.ServicoCatalogoId,
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

        return itens
            .OrderBy(x => x.Ordem <= 0 ? int.MaxValue : x.Ordem)
            .ThenBy(x => x.CreatedAt)
            .ToList();
    }

    public async Task<OrdemServicoItemDto?> AtualizarAsync(
        Guid empresaId,
        Guid ordemServicoId,
        Guid itemId,
        UpdateOrdemServicoItemDto dto,
        CancellationToken cancellationToken = default)
    {
        var ordemServico = await _context.OrdensServico
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == ordemServicoId, cancellationToken);

        if (ordemServico is null)
            return null;

        if (ordemServico.Status == "CANCELADA" || ordemServico.Status == "ENTREGUE")
            throw new InvalidOperationException("Não é possível alterar itens de uma OS cancelada ou entregue.");

        var item = ordemServico.Itens.FirstOrDefault(x => x.Id == itemId);

        if (item is null)
            return null;

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

        RecalcularTotais(ordemServico);

        await _context.SaveChangesAsync(cancellationToken);

        return Map(item);
    }

    public async Task<bool> RemoverAsync(
        Guid empresaId,
        Guid ordemServicoId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var ordemServico = await _context.OrdensServico
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == ordemServicoId, cancellationToken);

        if (ordemServico is null)
            return false;

        if (ordemServico.Status == "CANCELADA" || ordemServico.Status == "ENTREGUE")
            throw new InvalidOperationException("Não é possível remover itens de uma OS cancelada ou entregue.");

        var item = ordemServico.Itens.FirstOrDefault(x => x.Id == itemId);

        if (item is null)
            return false;

        _context.OrdensServicoItens.Remove(item);
        ordemServico.Itens.Remove(item);

        RecalcularTotais(ordemServico);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ReordenarAsync(
        Guid empresaId,
        Guid ordemServicoId,
        List<ReordenarOrdemServicoItemDto> itens,
        CancellationToken cancellationToken = default)
    {
        if (itens.Count == 0)
            return true;

        var ordemServico = await _context.OrdensServico
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == ordemServicoId, cancellationToken);

        if (ordemServico is null)
            return false;

        if (ordemServico.Status == "CANCELADA" || ordemServico.Status == "ENTREGUE")
            throw new InvalidOperationException("Não é possível reordenar itens de uma OS cancelada ou entregue.");

        var ids = itens.Select(x => x.Id).ToHashSet();
        var entities = ordemServico.Itens.Where(x => ids.Contains(x.Id)).ToList();

        if (entities.Count != ids.Count)
            return false;

        foreach (var entity in entities)
        {
            var dto = itens.First(x => x.Id == entity.Id);
            entity.Ordem = dto.Ordem <= 0 ? 1 : dto.Ordem;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task PreencherItemAsync(
        Guid empresaId,
        OrdemServicoItem item,
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
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x => x.EmpresaId == empresaId && x.Id == pecaId.Value && x.Ativo,
                            cancellationToken);

                    if (peca is null)
                        throw new InvalidOperationException("Peça não encontrada.");

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

    private static void RecalcularTotais(OrdemServico ordemServico)
    {
        ordemServico.ValorPecas = ordemServico.Itens
            .Where(x => x.TipoItem == "PECA")
            .Sum(x => x.ValorTotal);

        ordemServico.ValorTotal = ordemServico.ValorMaoObra + ordemServico.ValorPecas - ordemServico.Desconto;

        if (ordemServico.ValorTotal < 0)
            ordemServico.ValorTotal = 0;
    }

    private static OrdemServicoItemDto Map(OrdemServicoItem entity)
    {
        return new OrdemServicoItemDto
        {
            Id = entity.Id,
            EmpresaId = entity.EmpresaId,
            OrdemServicoId = entity.OrdemServicoId,
            TipoItem = entity.TipoItem,
            Ordem = entity.Ordem,
            ServicoCatalogoId = entity.ServicoCatalogoId,
            PecaId = entity.PecaId,
            Descricao = entity.Descricao,
            Quantidade = entity.Quantidade,
            CustoUnitario = entity.CustoUnitario,
            ValorUnitario = entity.ValorUnitario,
            Desconto = entity.Desconto,
            ValorTotal = entity.ValorTotal,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
