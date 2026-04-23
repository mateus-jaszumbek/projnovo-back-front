using ServicosApp.Domain.Entities;
using ServicosApp.Application.DTOs.Fiscal;

namespace ServicosApp.Infrastructure.Services;

public class NfseProviderClientFake : INfseProviderClient
{
    public Task<NfseProviderResult> EmitirAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa credencial,
        DocumentoFiscal documento,
        CancellationToken cancellationToken = default)
    {
        var chave = $"NFSE-{documento.EmpresaId:N}-{documento.Serie}-{documento.Numero}";
        var protocolo = $"PROTO-{Guid.NewGuid():N}";
        var codigoVerificacao = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        var lote = $"LOTE-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var xml = $"""
        <nfse>
          <numero>{documento.Numero}</numero>
          <serie>{documento.Serie}</serie>
          <cliente>{System.Security.SecurityElement.Escape(documento.ClienteNome)}</cliente>
          <valorTotal>{documento.ValorTotal:F2}</valorTotal>
          <emitidaEm>{DateTime.UtcNow:O}</emitidaEm>
        </nfse>
        """;

        return Task.FromResult(new NfseProviderResult
        {
            Sucesso = true,
            Status = "AUTORIZADO",
            NumeroExterno = documento.Numero.ToString(),
            ChaveAcesso = chave,
            Protocolo = protocolo,
            CodigoVerificacao = codigoVerificacao,
            LinkConsulta = $"/mock/nfse/consulta/{chave}",
            Lote = lote,
            XmlConteudo = xml,
            XmlUrl = $"/mock/nfse/xml/{documento.Id}",
            PdfUrl = $"/mock/nfse/pdf/{documento.Id}",
            RequestPayload = $"{{ \"empresaId\": \"{documento.EmpresaId}\", \"documentoFiscalId\": \"{documento.Id}\" }}",
            ResponsePayload = $"{{ \"sucesso\": true, \"protocolo\": \"{protocolo}\" }}"
        });
    }

    public Task<NfseProviderResult> ConsultarAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa credencial,
        DocumentoFiscal documento,
        CancellationToken cancellationToken = default)
    {
        var sucesso = !string.IsNullOrWhiteSpace(documento.Protocolo);

        return Task.FromResult(new NfseProviderResult
        {
            Sucesso = sucesso,
            Status = sucesso ? "AUTORIZADO" : "REJEITADO",
            NumeroExterno = documento.Numero.ToString(),
            ChaveAcesso = documento.ChaveAcesso,
            Protocolo = documento.Protocolo,
            CodigoVerificacao = documento.CodigoVerificacao,
            LinkConsulta = documento.LinkConsulta,
            Lote = documento.Lote,
            XmlConteudo = documento.XmlConteudo,
            XmlUrl = documento.XmlUrl,
            PdfUrl = documento.PdfUrl,
            CodigoErro = sucesso ? null : "CONSULTA001",
            MensagemErro = sucesso ? null : "Documento não localizado no provedor fake.",
            RequestPayload = $"{{ \"acao\": \"consultar\", \"documentoFiscalId\": \"{documento.Id}\" }}",
            ResponsePayload = sucesso
                ? "{ \"status\": \"AUTORIZADO\" }"
                : "{ \"status\": \"REJEITADO\" }"
        });
    }

    public Task<NfseProviderResult> CancelarAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa credencial,
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
                CodigoErro = "CANCEL001",
                MensagemErro = "Documento sem protocolo para cancelamento.",
                RequestPayload = $"{{ \"acao\": \"cancelar\", \"motivo\": \"{motivo}\" }}",
                ResponsePayload = "{ \"sucesso\": false }"
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
            XmlConteudo = documento.XmlConteudo,
            XmlUrl = documento.XmlUrl,
            PdfUrl = documento.PdfUrl,
            RequestPayload = $"{{ \"acao\": \"cancelar\", \"motivo\": \"{motivo}\" }}",
            ResponsePayload = "{ \"sucesso\": true, \"status\": \"CANCELADO\" }"
        });
    }
}