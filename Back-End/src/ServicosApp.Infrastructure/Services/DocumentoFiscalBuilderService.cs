using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class DocumentoFiscalBuilderService : IDocumentoFiscalBuilderService
{
    private readonly AppDbContext _context;
    private readonly INumeracaoFiscalService _numeracaoFiscalService;

    public DocumentoFiscalBuilderService(
        AppDbContext context,
        INumeracaoFiscalService numeracaoFiscalService)
    {
        _context = context;
        _numeracaoFiscalService = numeracaoFiscalService;
    }

    public async Task<DocumentoFiscal> CriarNfsePorOrdemServicoAsync(
        Guid empresaId,
        Guid usuarioId,
        Guid ordemServicoId,
        DateTime? dataCompetencia,
        string? observacoesNota,
        CancellationToken cancellationToken = default)
    {
        await ValidarDuplicidadeAsync(
            empresaId,
            TipoDocumentoFiscal.Nfse,
            OrigemDocumentoFiscal.OrdemServico,
            ordemServicoId,
            cancellationToken);

        var ordemServico = await _context.OrdensServico
            .Include(x => x.Cliente)
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.Id == ordemServicoId && x.EmpresaId == empresaId, cancellationToken);

        if (ordemServico is null)
            throw new KeyNotFoundException("Ordem de serviço não encontrada.");

        var status = (ordemServico.Status ?? string.Empty).Trim().ToUpperInvariant();
        if (status != "FINALIZADA" && status != "ENTREGUE" && status != "CONCLUIDA")
            throw new InvalidOperationException("A OS precisa estar finalizada ou entregue para emitir NFS-e.");

        if (ordemServico.Cliente is null)
            throw new InvalidOperationException("Cliente da ordem de serviço não encontrado.");

        var config = await ObterConfiguracaoFiscalAtivaAsync(empresaId, cancellationToken);

        var itensServico = ordemServico.Itens
            .Where(x => string.Equals(x.TipoItem, "SERVICO", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!itensServico.Any())
            throw new InvalidOperationException("A ordem de serviço não possui itens de serviço para emissão.");

        var (serie, numero, serieRps, numeroRps) =
            await _numeracaoFiscalService.ReservarNumeracaoNfseAsync(empresaId, cancellationToken);

        var valorServicos = itensServico.Sum(x => x.ValorTotal);
        var desconto = itensServico.Sum(x => x.Desconto);

        var documento = new DocumentoFiscal
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            TipoDocumento = TipoDocumentoFiscal.Nfse,
            OrigemTipo = OrigemDocumentoFiscal.OrdemServico,
            OrigemId = ordemServico.Id,
            Numero = numero,
            Serie = serie,
            SerieRps = serieRps,
            NumeroRps = numeroRps,
            Status = StatusDocumentoFiscal.PendenteEnvio,
            Ambiente = config.Ambiente,
            ClienteId = ordemServico.ClienteId,
            ClienteNome = ordemServico.Cliente.Nome,
            ClienteCpfCnpj = ordemServico.Cliente.CpfCnpj,
            ClienteEmail = ordemServico.Cliente.Email,
            ClienteTelefone = ordemServico.Cliente.Telefone,
            ClienteCep = ordemServico.Cliente.Cep,
            ClienteLogradouro = ordemServico.Cliente.Logradouro,
            ClienteNumero = ordemServico.Cliente.Numero,
            ClienteComplemento = ordemServico.Cliente.Complemento,
            ClienteBairro = ordemServico.Cliente.Bairro,
            ClienteCidade = ordemServico.Cliente.Cidade,
            ClienteUf = ordemServico.Cliente.Uf,
            DataEmissao = DateTime.UtcNow,
            DataCompetencia = dataCompetencia ?? DateTime.UtcNow,
            ValorServicos = valorServicos,
            ValorProdutos = 0m,
            Desconto = desconto,
            ValorTotal = valorServicos,
            CreatedBy = usuarioId,
            PayloadEnvio = observacoesNota
        };

        documento.Itens = itensServico.Select(item => new DocumentoFiscalItem
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            DocumentoFiscalId = documento.Id,
            TipoItem = TipoItemFiscal.Servico,
            ServicoCatalogoId = item.ServicoCatalogoId,
            Descricao = item.Descricao,
            Quantidade = item.Quantidade,
            ValorUnitario = item.ValorUnitario,
            Desconto = item.Desconto,
            ValorTotal = item.ValorTotal,
            Cnae = config.CnaePrincipal,
            ItemListaServico = config.ItemListaServico,
            BaseIss = item.ValorTotal,
            AliquotaIss = config.AliquotaIssPadrao ?? 0m,
            ValorIss = CalcularValor(item.ValorTotal, config.AliquotaIssPadrao ?? 0m),
            IssRetido = config.IssRetidoPadrao
        }).ToList();

        return documento;
    }

    public async Task<DocumentoFiscal> CriarDfePorVendaAsync(
        Guid empresaId,
        Guid usuarioId,
        Guid vendaId,
        TipoDocumentoFiscal tipoDocumento,
        DateTime? dataEmissao,
        string? observacoesNota,
        bool validarTributacaoCompleta,
        CancellationToken cancellationToken = default)
    {
        if (tipoDocumento != TipoDocumentoFiscal.Nfe && tipoDocumento != TipoDocumentoFiscal.Nfce)
            throw new InvalidOperationException("A emissão por venda suporta apenas NF-e ou NFC-e.");

        await ValidarDuplicidadeAsync(
            empresaId,
            tipoDocumento,
            OrigemDocumentoFiscal.Venda,
            vendaId,
            cancellationToken);

        var empresa = await _context.Empresas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == empresaId && x.Ativo, cancellationToken);

        if (empresa is null)
            throw new InvalidOperationException("Empresa não encontrada.");

        var venda = await _context.Vendas
            .Include(x => x.Cliente)
            .Include(x => x.Itens)
                .ThenInclude(x => x.Peca)
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == vendaId, cancellationToken);

        if (venda is null)
            throw new KeyNotFoundException("Venda não encontrada.");

        if (!string.Equals(venda.Status, "FECHADA", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A venda precisa estar fechada para emitir NF-e/NFC-e.");

        if (!venda.Itens.Any())
            throw new InvalidOperationException("A venda não possui itens para emissão.");

        if (tipoDocumento == TipoDocumentoFiscal.Nfe)
            ValidarDestinatarioNfe(venda.Cliente);

        var config = await ObterConfiguracaoFiscalAtivaAsync(empresaId, cancellationToken);
        var (serie, numero) = await _numeracaoFiscalService.ReservarNumeracaoAsync(
            empresaId,
            tipoDocumento,
            cancellationToken);

        var documento = new DocumentoFiscal
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            TipoDocumento = tipoDocumento,
            OrigemTipo = OrigemDocumentoFiscal.Venda,
            OrigemId = venda.Id,
            Numero = numero,
            Serie = serie,
            Status = StatusDocumentoFiscal.PendenteEnvio,
            Ambiente = config.Ambiente,
            ClienteId = venda.ClienteId,
            ClienteNome = venda.Cliente?.Nome ?? "CONSUMIDOR NAO IDENTIFICADO",
            ClienteCpfCnpj = venda.Cliente?.CpfCnpj,
            ClienteEmail = venda.Cliente?.Email,
            ClienteTelefone = venda.Cliente?.Telefone,
            ClienteCep = venda.Cliente?.Cep,
            ClienteLogradouro = venda.Cliente?.Logradouro,
            ClienteNumero = venda.Cliente?.Numero,
            ClienteComplemento = venda.Cliente?.Complemento,
            ClienteBairro = venda.Cliente?.Bairro,
            ClienteCidade = venda.Cliente?.Cidade,
            ClienteUf = venda.Cliente?.Uf,
            DataEmissao = dataEmissao ?? DateTime.UtcNow,
            ValorServicos = 0m,
            ValorProdutos = venda.Itens.Sum(x => x.ValorTotal),
            Desconto = venda.Desconto,
            ValorTotal = venda.ValorTotal,
            CreatedBy = usuarioId,
            PayloadEnvio = observacoesNota
        };

        var errosTributarios = new List<string>();
        var ufOrigem = NormalizarUf(empresa.Uf);
        var ufDestino = NormalizarUf(venda.Cliente?.Uf) ?? ufOrigem;

        foreach (var item in venda.Itens)
        {
            if (item.Peca is null)
            {
                errosTributarios.Add($"Item {item.Descricao}: peça não encontrada.");
                continue;
            }

            var regra = await ResolverRegraFiscalAsync(
                empresaId,
                tipoDocumento,
                ufOrigem,
                ufDestino,
                config.RegimeTributario,
                item.Peca.Ncm,
                cancellationToken);

            var ncm = Normalizar(item.Peca.Ncm);
            var cfop = Normalizar(regra?.Cfop) ??
                       Normalizar(tipoDocumento == TipoDocumentoFiscal.Nfe
                           ? item.Peca.CfopPadraoNfe
                           : item.Peca.CfopPadraoNfce);
            var cstCsosn = Normalizar(regra?.CstCsosn) ?? Normalizar(item.Peca.CstCsosn);
            var origemMercadoria = Normalizar(regra?.OrigemMercadoria) ?? Normalizar(item.Peca.OrigemMercadoria);

            if (validarTributacaoCompleta)
                ValidarTributacaoItem(item, tipoDocumento, ncm, cfop, cstCsosn, origemMercadoria, errosTributarios);

            var aliquotaIcms = regra?.AliquotaIcms ?? 0m;
            var aliquotaPis = regra?.AliquotaPis ?? 0m;
            var aliquotaCofins = regra?.AliquotaCofins ?? 0m;

            documento.Itens.Add(new DocumentoFiscalItem
            {
                Id = Guid.NewGuid(),
                EmpresaId = empresaId,
                DocumentoFiscalId = documento.Id,
                TipoItem = TipoItemFiscal.Produto,
                PecaId = item.PecaId,
                Descricao = item.Descricao,
                Quantidade = item.Quantidade,
                ValorUnitario = item.ValorUnitario,
                Desconto = item.Desconto,
                ValorTotal = item.ValorTotal,
                Ncm = ncm,
                Cfop = cfop,
                Cest = Normalizar(regra?.Cest) ?? Normalizar(item.Peca.Cest),
                CstCsosn = cstCsosn,
                OrigemMercadoria = origemMercadoria,
                BaseIcms = item.ValorTotal,
                AliquotaIcms = aliquotaIcms,
                ValorIcms = CalcularValor(item.ValorTotal, aliquotaIcms),
                BasePis = item.ValorTotal,
                AliquotaPis = aliquotaPis,
                ValorPis = CalcularValor(item.ValorTotal, aliquotaPis),
                BaseCofins = item.ValorTotal,
                AliquotaCofins = aliquotaCofins,
                ValorCofins = CalcularValor(item.ValorTotal, aliquotaCofins)
            });
        }

        if (errosTributarios.Any())
            throw new InvalidOperationException("Revise a tributação antes de emitir: " + string.Join(" ", errosTributarios));

        return documento;
    }

    private async Task ValidarDuplicidadeAsync(
        Guid empresaId,
        TipoDocumentoFiscal tipoDocumento,
        OrigemDocumentoFiscal origemTipo,
        Guid origemId,
        CancellationToken cancellationToken)
    {
        var existe = await _context.DocumentosFiscais
            .AsNoTracking()
            .AnyAsync(x =>
                x.EmpresaId == empresaId &&
                x.TipoDocumento == tipoDocumento &&
                x.OrigemTipo == origemTipo &&
                x.OrigemId == origemId &&
                x.Status != StatusDocumentoFiscal.Cancelado,
                cancellationToken);

        if (existe)
            throw new InvalidOperationException("Já existe documento fiscal ativo para esta origem.");
    }

    private async Task<ConfiguracaoFiscal> ObterConfiguracaoFiscalAtivaAsync(
        Guid empresaId,
        CancellationToken cancellationToken)
    {
        return await _context.ConfiguracoesFiscais
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Ativo, cancellationToken)
            ?? throw new InvalidOperationException("Configuração fiscal não encontrada.");
    }

    private async Task<RegraFiscalProduto?> ResolverRegraFiscalAsync(
        Guid empresaId,
        TipoDocumentoFiscal tipoDocumento,
        string? ufOrigem,
        string? ufDestino,
        string regimeTributario,
        string? ncm,
        CancellationToken cancellationToken)
    {
        var regras = await _context.RegrasFiscaisProdutos
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId &&
                        x.Ativo &&
                        x.TipoDocumentoFiscal == tipoDocumento)
            .ToListAsync(cancellationToken);

        var ncmNormalizado = Normalizar(ncm);
        var regimeNormalizado = Normalizar(regimeTributario);

        return regras
            .Where(x => CampoCombina(x.UfOrigem, ufOrigem))
            .Where(x => CampoCombina(x.UfDestino, ufDestino))
            .Where(x => CampoCombina(x.RegimeTributario, regimeNormalizado))
            .Where(x => CampoCombina(x.Ncm, ncmNormalizado))
            .OrderByDescending(Especificidade)
            .FirstOrDefault();
    }

    private static void ValidarDestinatarioNfe(Cliente? cliente)
    {
        if (cliente is null)
            throw new InvalidOperationException("NF-e exige cliente/destinatário informado.");

        if (string.IsNullOrWhiteSpace(cliente.CpfCnpj))
            throw new InvalidOperationException("NF-e exige CPF/CNPJ do destinatário.");

        if (string.IsNullOrWhiteSpace(cliente.Uf))
            throw new InvalidOperationException("NF-e exige UF do destinatário.");
    }

    private static void ValidarTributacaoItem(
        VendaItem item,
        TipoDocumentoFiscal tipoDocumento,
        string? ncm,
        string? cfop,
        string? cstCsosn,
        string? origemMercadoria,
        List<string> erros)
    {
        if (string.IsNullOrWhiteSpace(ncm))
            erros.Add($"Item {item.Descricao}: informe NCM.");

        if (string.IsNullOrWhiteSpace(cfop))
            erros.Add($"Item {item.Descricao}: informe CFOP para {tipoDocumento}.");

        if (string.IsNullOrWhiteSpace(cstCsosn))
            erros.Add($"Item {item.Descricao}: informe CST/CSOSN.");

        if (string.IsNullOrWhiteSpace(origemMercadoria))
            erros.Add($"Item {item.Descricao}: informe origem da mercadoria.");
    }

    private static decimal CalcularValor(decimal baseCalculo, decimal aliquota)
        => Math.Round(baseCalculo * aliquota / 100m, 2, MidpointRounding.AwayFromZero);

    private static bool CampoCombina(string? regra, string? valor)
        => string.IsNullOrWhiteSpace(regra) ||
           string.Equals(Normalizar(regra), Normalizar(valor), StringComparison.OrdinalIgnoreCase);

    private static int Especificidade(RegraFiscalProduto regra)
    {
        var pontos = 0;
        if (!string.IsNullOrWhiteSpace(regra.UfOrigem)) pontos++;
        if (!string.IsNullOrWhiteSpace(regra.UfDestino)) pontos++;
        if (!string.IsNullOrWhiteSpace(regra.RegimeTributario)) pontos++;
        if (!string.IsNullOrWhiteSpace(regra.Ncm)) pontos++;
        return pontos;
    }

    private static string? Normalizar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string? NormalizarUf(string? uf)
        => string.IsNullOrWhiteSpace(uf) ? null : uf.Trim().ToUpperInvariant();
}