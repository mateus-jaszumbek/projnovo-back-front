using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class DocumentoFiscalArquivoService : IDocumentoFiscalArquivoService
{
    private readonly AppDbContext _context;
    private readonly HttpClient _httpClient;

    public DocumentoFiscalArquivoService(AppDbContext context)
        : this(context, new HttpClient())
    {
    }

    public DocumentoFiscalArquivoService(AppDbContext context, HttpClient httpClient)
    {
        _context = context;
        _httpClient = httpClient;
    }

    public async Task<DocumentoFiscalArquivoDto?> ObterXmlAsync(
        Guid empresaId,
        Guid documentoFiscalId,
        CancellationToken cancellationToken = default)
    {
        var documento = await _context.DocumentosFiscais
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.EmpresaId == empresaId && x.Id == documentoFiscalId,
                cancellationToken);

        if (documento is null)
            return null;

        var xmlConteudo = documento.XmlConteudo;
        if (string.IsNullOrWhiteSpace(xmlConteudo))
            xmlConteudo = await ObterConteudoXmlRemotoAsync(documento.XmlUrl, cancellationToken);

        if (string.IsNullOrWhiteSpace(xmlConteudo))
            return null;

        return new DocumentoFiscalArquivoDto
        {
            FileName = $"{MontarSlugDocumento(documento.TipoDocumento.ToString())}-{documento.Serie}-{documento.Numero}.xml",
            ContentType = "application/xml",
            Conteudo = xmlConteudo
        };
    }

    public async Task<DocumentoFiscalImpressaoDto?> ObterImpressaoAsync(
        Guid empresaId,
        Guid documentoFiscalId,
        CancellationToken cancellationToken = default)
    {
        var documento = await _context.DocumentosFiscais
            .AsNoTracking()
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(
                x => x.EmpresaId == empresaId && x.Id == documentoFiscalId,
                cancellationToken);

        if (documento is null)
            return null;

        var empresa = await _context.Empresas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == empresaId, cancellationToken);

        if (empresa is null)
            return null;

        return new DocumentoFiscalImpressaoDto
        {
            Id = documento.Id,
            TipoDocumento = documento.TipoDocumento.ToString(),
            Numero = documento.Numero,
            Serie = documento.Serie,
            Status = documento.Status.ToString(),
            Ambiente = documento.Ambiente.ToString(),
            DataEmissao = documento.DataEmissao,
            DataAutorizacao = documento.DataAutorizacao,
            EmpresaRazaoSocial = empresa.RazaoSocial,
            EmpresaNomeFantasia = empresa.NomeFantasia,
            EmpresaCnpj = empresa.Cnpj,
            EmpresaTelefone = empresa.Telefone,
            EmpresaEmail = empresa.Email,
            EmpresaLogoUrl = empresa.LogoUrl,
            EmpresaEnderecoCompleto = MontarEndereco(
                empresa.Logradouro,
                empresa.Numero,
                empresa.Complemento,
                empresa.Bairro,
                empresa.Cidade,
                empresa.Uf,
                empresa.Cep),
            ClienteNome = documento.ClienteNome,
            ClienteCpfCnpj = documento.ClienteCpfCnpj,
            ClienteTelefone = documento.ClienteTelefone,
            ClienteEmail = documento.ClienteEmail,
            ClienteEnderecoCompleto = MontarEndereco(
                documento.ClienteLogradouro,
                documento.ClienteNumero,
                documento.ClienteComplemento,
                documento.ClienteBairro,
                documento.ClienteCidade,
                documento.ClienteUf,
                documento.ClienteCep),
            ChaveAcesso = documento.ChaveAcesso,
            Protocolo = documento.Protocolo,
            CodigoVerificacao = documento.CodigoVerificacao,
            OfficialPdfUrl = documento.PdfUrl,
            ValorServicos = documento.ValorServicos,
            ValorProdutos = documento.ValorProdutos,
            Desconto = documento.Desconto,
            ValorTotal = documento.ValorTotal,
            Itens = documento.Itens
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Descricao)
                .Select(x => new DocumentoFiscalImpressaoItemDto
                {
                    TipoItem = x.TipoItem.ToString(),
                    Descricao = x.Descricao,
                    Quantidade = x.Quantidade,
                    ValorUnitario = x.ValorUnitario,
                    Desconto = x.Desconto,
                    ValorTotal = x.ValorTotal,
                    Ncm = x.Ncm,
                    Cfop = x.Cfop,
                    CstCsosn = x.CstCsosn,
                    ItemListaServico = x.ItemListaServico
                })
                .ToList()
        };
    }

    private static string MontarSlugDocumento(string value)
    {
        return string.Concat(
            value
                .Trim()
                .ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-'))
            .Trim('-');
    }

    private static string? MontarEndereco(
        string? logradouro,
        string? numero,
        string? complemento,
        string? bairro,
        string? cidade,
        string? uf,
        string? cep)
    {
        var linhaPrincipal = new[]
        {
            logradouro,
            numero,
            complemento
        }
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x!.Trim());

        var linhaSecundaria = new[]
        {
            bairro,
            cidade,
            uf,
            cep
        }
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x!.Trim());

        var partes = new List<string>();
        var endereco = string.Join(", ", linhaPrincipal);
        var localidade = string.Join(" - ", linhaSecundaria);

        if (!string.IsNullOrWhiteSpace(endereco))
            partes.Add(endereco);

        if (!string.IsNullOrWhiteSpace(localidade))
            partes.Add(localidade);

        return partes.Count == 0 ? null : string.Join(" | ", partes);
    }

    private async Task<string?> ObterConteudoXmlRemotoAsync(
        string? xmlUrl,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(xmlUrl, UriKind.Absolute, out var absoluteUri))
            return null;

        try
        {
            using var response = await _httpClient.GetAsync(absoluteUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }
}
