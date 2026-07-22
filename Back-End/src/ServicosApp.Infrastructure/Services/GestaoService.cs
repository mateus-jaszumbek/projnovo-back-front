using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class GestaoService : IGestaoService
{
    private readonly AppDbContext _context;

    public GestaoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task RegistrarCompraPecaAsync(Guid empresaId, Guid? usuarioId, CompraPecaDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Quantidade <= 0)
            throw new InvalidOperationException("Quantidade deve ser maior que zero.");

        if (dto.CustoUnitario < 0)
            throw new InvalidOperationException("Custo unitário não pode ser negativo.");

        var peca = await _context.Pecas
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == dto.PecaId && x.Ativo, cancellationToken);

        if (peca is null)
            throw new InvalidOperationException("Peça não encontrada.");

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var total = dto.Quantidade * dto.CustoUnitario;
        peca.EstoqueAtual += dto.Quantidade;
        peca.CustoUnitario = dto.CustoUnitario;

        _context.EstoqueMovimentos.Add(new EstoqueMovimento
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            PecaId = peca.Id,
            TipoMovimento = "COMPRA",
            OrigemTipo = "COMPRA_PECA",
            OrigemId = peca.Id,
            Quantidade = dto.Quantidade,
            CustoUnitario = dto.CustoUnitario,
            Observacao = string.IsNullOrWhiteSpace(dto.Observacoes) ? $"Compra de {peca.Nome}" : dto.Observacoes.Trim(),
            CreatedBy = usuarioId,
            DataMovimento = DateTime.UtcNow
        });

        if (dto.GerarContaPagar && total > 0)
        {
            var fornecedor = await ObterFornecedorAsync(empresaId, dto.FornecedorId, cancellationToken);
            _context.ContasPagar.Add(new ContaPagar
            {
                Id = Guid.NewGuid(),
                EmpresaId = empresaId,
                Descricao = $"Compra de {peca.Nome}",
                FornecedorId = fornecedor?.Id,
                Fornecedor = string.IsNullOrWhiteSpace(dto.Fornecedor) ? fornecedor?.Nome : dto.Fornecedor.Trim(),
                Categoria = "COMPRA_PECAS",
                DataEmissao = DateOnly.FromDateTime(DateTime.UtcNow),
                DataVencimento = dto.DataVencimento,
                Valor = total,
                ValorPago = 0,
                Status = "PENDENTE",
                Observacoes = string.IsNullOrWhiteSpace(dto.Observacoes) ? null : dto.Observacoes.Trim(),
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
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

    public async Task<DreGerencialDto> ObterDreAsync(Guid empresaId, DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default)
    {
        var inicioDate = inicio?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue;
        var fimDate = (fim?.ToDateTime(TimeOnly.MaxValue) ?? DateTime.MaxValue);

        var vendas = await _context.Vendas
            .AsNoTracking()
            .Include(x => x.Itens)
            .Where(x => x.EmpresaId == empresaId && x.Status == "FECHADA" && x.DataVenda >= inicioDate && x.DataVenda <= fimDate)
            .ToListAsync(cancellationToken);

        var receitaOs = await _context.OrdensServico
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId
                && (x.Status == "PRONTA" || x.Status == "ENTREGUE")
                && x.DataConclusao.HasValue
                && x.DataConclusao.Value >= inicioDate
                && x.DataConclusao.Value <= fimDate)
            .SumAsync(x => x.ValorTotal, cancellationToken);

        var receita = vendas.Sum(x => x.ValorTotal) + receitaOs;
        var custo = vendas.Sum(x => x.Itens.Sum(item => item.CustoUnitario * item.Quantidade));

        var inicioOnly = inicio ?? DateOnly.MinValue;
        var fimOnly = fim ?? DateOnly.MaxValue;

        var despesasPagas = await _context.ContasPagar
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.DataVencimento >= inicioOnly && x.DataVencimento <= fimOnly)
            .SumAsync(x => x.ValorPago, cancellationToken);

        var despesasPendentes = await _context.ContasPagar
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Status != "PAGO" && x.DataVencimento >= inicioOnly && x.DataVencimento <= fimOnly)
            .SumAsync(x => x.Valor - x.ValorPago, cancellationToken);

        var lucroBruto = receita - custo;
        var lucroLiquido = lucroBruto - despesasPagas;

        return new DreGerencialDto
        {
            ReceitaBruta = receita,
            CustoPecas = custo,
            DespesasPagas = despesasPagas,
            DespesasPendentes = despesasPendentes,
            LucroBruto = lucroBruto,
            LucroLiquido = lucroLiquido,
            MargemBrutaPercentual = receita > 0 ? Math.Round((lucroBruto / receita) * 100, 2) : 0,
            MargemLiquidaPercentual = receita > 0 ? Math.Round((lucroLiquido / receita) * 100, 2) : 0
        };
    }

    public async Task<List<ComissaoDto>> ListarComissoesAsync(Guid empresaId, DateOnly? inicio, DateOnly? fim, decimal percentualVendas, decimal percentualServicos, CancellationToken cancellationToken = default)
    {
        var inicioDate = inicio?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue;
        var fimDate = fim?.ToDateTime(TimeOnly.MaxValue) ?? DateTime.MaxValue;

        var vendas = await _context.Vendas
            .AsNoTracking()
            .Include(x => x.UsuarioCriacao)
            .Where(x => x.EmpresaId == empresaId && x.Status == "FECHADA" && x.DataVenda >= inicioDate && x.DataVenda <= fimDate)
            .GroupBy(x => new { x.CreatedBy, Nome = x.UsuarioCriacao != null ? x.UsuarioCriacao.Nome : "Sem vendedor" })
            .Select(g => new ComissaoDto
            {
                Tipo = "VENDEDOR",
                PessoaId = g.Key.CreatedBy,
                PessoaNome = g.Key.Nome,
                BaseCalculo = g.Sum(x => x.ValorTotal),
                Percentual = percentualVendas,
                ValorComissao = g.Sum(x => x.ValorTotal) * percentualVendas / 100
            })
            .ToListAsync(cancellationToken);

        var servicos = await _context.OrdensServico
            .AsNoTracking()
            .Include(x => x.Tecnico)
            .Where(x => x.EmpresaId == empresaId && x.DataEntrada >= inicioDate && x.DataEntrada <= fimDate && x.Status != "CANCELADA")
            .GroupBy(x => new { x.TecnicoId, Nome = x.Tecnico != null ? x.Tecnico.Nome : "Sem técnico" })
            .Select(g => new ComissaoDto
            {
                Tipo = "TECNICO",
                PessoaId = g.Key.TecnicoId,
                PessoaNome = g.Key.Nome,
                BaseCalculo = g.Sum(x => x.ValorMaoObra),
                Percentual = percentualServicos,
                ValorComissao = g.Sum(x => x.ValorMaoObra) * percentualServicos / 100
            })
            .ToListAsync(cancellationToken);

        return vendas.Concat(servicos).OrderBy(x => x.Tipo).ThenBy(x => x.PessoaNome).ToList();
    }

    public async Task<List<AuditoriaFinanceiraDto>> ListarAuditoriaFinanceiraAsync(Guid empresaId, DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default)
    {
        var inicioDate = inicio?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue;
        var fimDate = fim?.ToDateTime(TimeOnly.MaxValue) ?? DateTime.MaxValue;

        return await _context.CaixaLancamentos
            .AsNoTracking()
            .Include(x => x.UsuarioCriacao)
            .Where(x => x.EmpresaId == empresaId && x.CreatedAt >= inicioDate && x.CreatedAt <= fimDate)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AuditoriaFinanceiraDto
            {
                Data = x.CreatedAt,
                Tipo = x.Tipo,
                OrigemTipo = x.OrigemTipo ?? "MANUAL",
                OrigemId = x.OrigemId,
                FormaPagamento = x.FormaPagamento,
                Valor = x.Valor,
                Observacao = x.Observacao,
                UsuarioId = x.CreatedBy,
                UsuarioNome = x.UsuarioCriacao != null ? x.UsuarioCriacao.Nome : null
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DespesaCategoriaDto>> ListarDespesasPorCategoriaAsync(Guid empresaId, DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default)
    {
        var inicioOnly = inicio ?? DateOnly.MinValue;
        var fimOnly = fim ?? DateOnly.MaxValue;

        var contas = await _context.ContasPagar
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.DataVencimento >= inicioOnly && x.DataVencimento <= fimOnly)
            .Select(x => new { x.Categoria, x.Valor, x.ValorPago })
            .ToListAsync(cancellationToken);

        return contas
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Categoria) ? "Sem categoria" : x.Categoria!.Trim())
            .Select(g => new DespesaCategoriaDto
            {
                Categoria = g.Key,
                Quantidade = g.Count(),
                TotalValor = g.Sum(x => x.Valor),
                TotalPago = g.Sum(x => x.ValorPago),
                TotalPendente = g.Sum(x => x.Valor - x.ValorPago)
            })
            .OrderByDescending(x => x.TotalValor)
            .ToList();
    }

    public async Task<List<ResumoMensalDto>> ListarResumoMensalAsync(Guid empresaId, int meses, CancellationToken cancellationToken = default)
    {
        var quantidadeMeses = meses <= 0 ? 12 : meses;
        var hoje = DateTime.UtcNow;
        var inicioPeriodo = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(quantidadeMeses - 1));
        var inicioPeriodoOnly = DateOnly.FromDateTime(inicioPeriodo);

        var vendas = await _context.Vendas
            .AsNoTracking()
            .Include(x => x.Itens)
            .Where(x => x.EmpresaId == empresaId && x.Status == "FECHADA" && x.DataVenda >= inicioPeriodo)
            .ToListAsync(cancellationToken);

        var ordensServico = await _context.OrdensServico
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId
                && (x.Status == "PRONTA" || x.Status == "ENTREGUE")
                && x.DataConclusao.HasValue
                && x.DataConclusao.Value >= inicioPeriodo)
            .Select(x => new { x.DataConclusao, x.ValorTotal })
            .ToListAsync(cancellationToken);

        var despesas = await _context.ContasPagar
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.DataVencimento >= inicioPeriodoOnly)
            .Select(x => new { x.DataVencimento, x.ValorPago })
            .ToListAsync(cancellationToken);

        var resultado = new List<ResumoMensalDto>();

        for (var i = 0; i < quantidadeMeses; i++)
        {
            var mesReferencia = inicioPeriodo.AddMonths(i);
            var ano = mesReferencia.Year;
            var mes = mesReferencia.Month;

            var vendasDoMes = vendas.Where(x => x.DataVenda.Year == ano && x.DataVenda.Month == mes).ToList();
            var receitaVendas = vendasDoMes.Sum(x => x.ValorTotal);
            var custoVendas = vendasDoMes.Sum(x => x.Itens.Sum(item => item.CustoUnitario * item.Quantidade));

            var receitaOs = ordensServico
                .Where(x => x.DataConclusao!.Value.Year == ano && x.DataConclusao.Value.Month == mes)
                .Sum(x => x.ValorTotal);

            var despesasPagasDoMes = despesas
                .Where(x => x.DataVencimento.Year == ano && x.DataVencimento.Month == mes)
                .Sum(x => x.ValorPago);

            var receita = receitaVendas + receitaOs;

            resultado.Add(new ResumoMensalDto
            {
                Ano = ano,
                Mes = mes,
                Receita = receita,
                Custo = custoVendas,
                Despesas = despesasPagasDoMes,
                LucroLiquido = receita - custoVendas - despesasPagasDoMes,
                QuantidadeVendas = vendasDoMes.Count
            });
        }

        return resultado;
    }

    public async Task<List<AniversarianteDto>> ListarAniversariantesAsync(
        Guid empresaId,
        int? mes,
        CancellationToken cancellationToken = default)
    {
        var mesFiltro = mes is >= 1 and <= 12 ? mes.Value : DateTime.UtcNow.Month;

        var clientes = await _context.Clientes
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Ativo && x.DataAniversario != null)
            .Select(x => new { x.Id, x.Nome, x.Telefone, x.Email, x.DataAniversario })
            .ToListAsync(cancellationToken);

        return clientes
            .Where(x => x.DataAniversario!.Value.Month == mesFiltro)
            .Select(x => new AniversarianteDto
            {
                ClienteId = x.Id,
                Nome = x.Nome,
                Telefone = x.Telefone,
                Email = x.Email,
                DataAniversario = x.DataAniversario!.Value,
                Dia = x.DataAniversario!.Value.Day,
                Mes = x.DataAniversario!.Value.Month
            })
            .OrderBy(x => x.Dia)
            .ToList();
    }

    public async Task<List<ClienteInativoDto>> ListarClientesInativosAsync(
        Guid empresaId,
        int mesesMin,
        int mesesMax,
        CancellationToken cancellationToken = default)
    {
        var min = mesesMin <= 0 ? 6 : mesesMin;
        var max = mesesMax <= min ? min + 6 : mesesMax;

        var hoje = DateTime.UtcNow;
        var limiteRecente = hoje.AddMonths(-min);
        var limiteAntigo = hoje.AddMonths(-max);

        var ultimasOs = await _context.OrdensServico
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .GroupBy(x => x.ClienteId)
            .Select(g => new { ClienteId = g.Key, Ultima = g.Max(x => x.DataEntrada) })
            .ToListAsync(cancellationToken);

        var ultimasVendas = await _context.Vendas
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.ClienteId != null)
            .GroupBy(x => x.ClienteId!.Value)
            .Select(g => new { ClienteId = g.Key, Ultima = g.Max(x => x.DataVenda) })
            .ToListAsync(cancellationToken);

        var ultimaVisitaPorCliente = new Dictionary<Guid, DateTime>();

        foreach (var item in ultimasOs)
            ultimaVisitaPorCliente[item.ClienteId] = item.Ultima;

        foreach (var item in ultimasVendas)
        {
            if (!ultimaVisitaPorCliente.TryGetValue(item.ClienteId, out var atual) || item.Ultima > atual)
                ultimaVisitaPorCliente[item.ClienteId] = item.Ultima;
        }

        var clienteIdsCandidatos = ultimaVisitaPorCliente
            .Where(kv => kv.Value <= limiteRecente && kv.Value >= limiteAntigo)
            .Select(kv => kv.Key)
            .ToList();

        if (clienteIdsCandidatos.Count == 0)
            return new List<ClienteInativoDto>();

        var clientes = await _context.Clientes
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Ativo && clienteIdsCandidatos.Contains(x.Id))
            .Select(x => new { x.Id, x.Nome, x.Telefone, x.Email })
            .ToListAsync(cancellationToken);

        return clientes
            .Select(x => new ClienteInativoDto
            {
                ClienteId = x.Id,
                Nome = x.Nome,
                Telefone = x.Telefone,
                Email = x.Email,
                UltimaVisita = ultimaVisitaPorCliente[x.Id],
                DiasSemContato = (int)(hoje - ultimaVisitaPorCliente[x.Id]).TotalDays
            })
            .OrderBy(x => x.UltimaVisita)
            .ToList();
    }
}
