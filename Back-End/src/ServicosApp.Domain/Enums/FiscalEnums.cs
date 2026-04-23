namespace ServicosApp.Domain.Enums;

public enum TipoDocumentoFiscal
{
    Nfse = 1,
    Nfe = 2,
    Nfce = 3
}

public enum OrigemDocumentoFiscal
{
    OrdemServico = 1,
    Venda = 2
}

public enum StatusDocumentoFiscal
{
    Rascunho = 1,
    PendenteEnvio = 2,
    Autorizado = 3,
    Rejeitado = 4,
    Cancelado = 5
}

public enum AmbienteFiscal
{
    Homologacao = 1,
    Producao = 2
}

public enum TipoItemFiscal
{
    Servico = 1,
    Produto = 2
}

public enum TipoEventoFiscal
{
    Emissao = 1,
    Cancelamento = 2,
    Consulta = 3,
    Rejeicao = 4,
    Sincronizacao = 5
}

public enum StatusEventoFiscal
{
    Processando = 1,
    Sucesso = 2,
    Erro = 3
}