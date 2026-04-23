using System.Globalization;
using System.Security;
using System.Text;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;

namespace ServicosApp.Infrastructure.Services;

public class DfeProviderClientFake : IDfeProviderClient
{
    public Task<NfseProviderResult> EmitirAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa? credencial,
        DocumentoFiscal documento,
        CancellationToken cancellationToken = default)
    {
        var chave = GerarChaveFake(documento);
        var protocolo = $"PROTO-{documento.TipoDocumento}-{Guid.NewGuid():N}";
        var lote = $"LOTE-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var xml = MontarXmlFake(documento, "AUTORIZADO");

        return Task.FromResult(new NfseProviderResult
        {
            Sucesso = true,
            Status = "AUTORIZADO",
            NumeroExterno = documento.Numero.ToString(CultureInfo.InvariantCulture),
            ChaveAcesso = chave,
            Protocolo = protocolo,
            CodigoVerificacao = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
            LinkConsulta = $"/mock/{documento.TipoDocumento.ToString().ToLowerInvariant()}/consulta/{chave}",
            Lote = lote,
            XmlConteudo = xml,
            XmlUrl = $"/mock/{documento.TipoDocumento.ToString().ToLowerInvariant()}/xml/{documento.Id}",
            PdfUrl = $"/mock/{documento.TipoDocumento.ToString().ToLowerInvariant()}/pdf/{documento.Id}",
            RequestPayload = $"{{ \"tipo\": \"{documento.TipoDocumento}\", \"documentoFiscalId\": \"{documento.Id}\" }}",
            ResponsePayload = $"{{ \"sucesso\": true, \"protocolo\": \"{protocolo}\", \"mock\": true }}"
        });
    }

    public Task<NfseProviderResult> ConsultarAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa? credencial,
        DocumentoFiscal documento,
        CancellationToken cancellationToken = default)
    {
        var sucesso = !string.IsNullOrWhiteSpace(documento.Protocolo);

        return Task.FromResult(new NfseProviderResult
        {
            Sucesso = sucesso,
            Status = sucesso ? "AUTORIZADO" : "REJEITADO",
            NumeroExterno = documento.Numero.ToString(CultureInfo.InvariantCulture),
            ChaveAcesso = documento.ChaveAcesso,
            Protocolo = documento.Protocolo,
            CodigoVerificacao = documento.CodigoVerificacao,
            LinkConsulta = documento.LinkConsulta,
            Lote = documento.Lote,
            XmlConteudo = documento.XmlConteudo,
            XmlUrl = documento.XmlUrl,
            PdfUrl = documento.PdfUrl,
            CodigoErro = sucesso ? null : "DFE_CONSULTA_001",
            MensagemErro = sucesso ? null : "Documento não localizado no provedor fake.",
            RequestPayload = $"{{ \"acao\": \"consultar\", \"documentoFiscalId\": \"{documento.Id}\" }}",
            ResponsePayload = sucesso
                ? "{ \"status\": \"AUTORIZADO\", \"mock\": true }"
                : "{ \"status\": \"REJEITADO\", \"mock\": true }"
        });
    }

    public Task<NfseProviderResult> CancelarAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa? credencial,
        DocumentoFiscal documento,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documento.Protocolo))
        {
            return Task.FromResult(new NfseProviderResult
            {
                Sucesso = false,
                Status = "ERRO",
                CodigoErro = "DFE_CANCEL_001",
                MensagemErro = "Documento sem protocolo para cancelamento.",
                RequestPayload = $"{{ \"acao\": \"cancelar\", \"motivo\": \"{SecurityElement.Escape(motivo)}\" }}",
                ResponsePayload = "{ \"sucesso\": false, \"mock\": true }"
            });
        }

        return Task.FromResult(new NfseProviderResult
        {
            Sucesso = true,
            Status = "CANCELADO",
            ChaveAcesso = documento.ChaveAcesso,
            Protocolo = documento.Protocolo,
            CodigoVerificacao = documento.CodigoVerificacao,
            LinkConsulta = documento.LinkConsulta,
            Lote = documento.Lote,
            XmlConteudo = MontarXmlFake(documento, "CANCELADO"),
            XmlUrl = documento.XmlUrl,
            PdfUrl = documento.PdfUrl,
            RequestPayload = $"{{ \"acao\": \"cancelar\", \"motivo\": \"{SecurityElement.Escape(motivo)}\" }}",
            ResponsePayload = "{ \"sucesso\": true, \"status\": \"CANCELADO\", \"mock\": true }"
        });
    }

    private static string GerarChaveFake(DocumentoFiscal documento)
    {
        var prefixo = documento.TipoDocumento == TipoDocumentoFiscal.Nfe ? "NFE" : "NFCE";
        return $"{prefixo}-{documento.EmpresaId:N}-{documento.Serie}-{documento.Numero}";
    }

    private static string MontarXmlFake(DocumentoFiscal documento, string status)
    {
        var tagRaiz = documento.TipoDocumento == TipoDocumentoFiscal.Nfe ? "nfeProc" : "nfceProc";
        var builder = new StringBuilder();

        builder.AppendLine($"<{tagRaiz} versao=\"mock\">");
        builder.AppendLine($"  <status>{SecurityElement.Escape(status)}</status>");
        builder.AppendLine($"  <numero>{documento.Numero}</numero>");
        builder.AppendLine($"  <serie>{documento.Serie}</serie>");
        builder.AppendLine($"  <destinatario>{SecurityElement.Escape(documento.ClienteNome)}</destinatario>");
        builder.AppendLine($"  <valorTotal>{documento.ValorTotal.ToString("F2", CultureInfo.InvariantCulture)}</valorTotal>");
        builder.AppendLine("  <itens>");

        foreach (var item in documento.Itens)
        {
            builder.AppendLine("    <item>");
            builder.AppendLine($"      <descricao>{SecurityElement.Escape(item.Descricao)}</descricao>");
            builder.AppendLine($"      <ncm>{SecurityElement.Escape(item.Ncm)}</ncm>");
            builder.AppendLine($"      <cfop>{SecurityElement.Escape(item.Cfop)}</cfop>");
            builder.AppendLine($"      <cstCsosn>{SecurityElement.Escape(item.CstCsosn)}</cstCsosn>");
            builder.AppendLine($"      <valor>{item.ValorTotal.ToString("F2", CultureInfo.InvariantCulture)}</valor>");
            builder.AppendLine("    </item>");
        }

        builder.AppendLine("  </itens>");
        builder.AppendLine($"</{tagRaiz}>");

        return builder.ToString();
    }
}
