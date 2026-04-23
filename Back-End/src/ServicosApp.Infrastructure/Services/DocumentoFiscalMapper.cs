using ServicosApp.Application.DTOs;
using ServicosApp.Domain.Entities;

namespace ServicosApp.Infrastructure.Services;

public static class DocumentoFiscalMapper
{
    public static DocumentoFiscalDto Map(DocumentoFiscal x)
    {
        return new DocumentoFiscalDto
        {
            Id = x.Id,
            EmpresaId = x.EmpresaId,
            TipoDocumento = x.TipoDocumento.ToString(),
            OrigemTipo = x.OrigemTipo.ToString(),
            OrigemId = x.OrigemId,
            Numero = x.Numero,
            Serie = x.Serie,
            SerieRps = x.SerieRps,
            NumeroRps = x.NumeroRps,
            Status = x.Status.ToString(),
            Ambiente = x.Ambiente.ToString(),
            ClienteId = x.ClienteId,
            ClienteNome = x.ClienteNome,
            ClienteCpfCnpj = x.ClienteCpfCnpj,
            DataEmissao = x.DataEmissao,
            DataCompetencia = x.DataCompetencia,
            DataAutorizacao = x.DataAutorizacao,
            DataCancelamento = x.DataCancelamento,
            ChaveAcesso = x.ChaveAcesso,
            Protocolo = x.Protocolo,
            CodigoVerificacao = x.CodigoVerificacao,
            LinkConsulta = x.LinkConsulta,
            NumeroExterno = x.NumeroExterno,
            Lote = x.Lote,
            ValorServicos = x.ValorServicos,
            ValorProdutos = x.ValorProdutos,
            Desconto = x.Desconto,
            ValorTotal = x.ValorTotal,
            XmlUrl = x.XmlUrl,
            PdfUrl = x.PdfUrl,
            CodigoRejeicao = x.CodigoRejeicao,
            MensagemRejeicao = x.MensagemRejeicao,
            MotivoCancelamento = x.MotivoCancelamento,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }
}
