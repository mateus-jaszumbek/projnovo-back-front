using ServicosApp.Domain.Enums;

namespace ServicosApp.Domain.Entities;

public class ConfiguracaoFiscal : EmpresaOwnedEntity
{
    public AmbienteFiscal Ambiente { get; set; } = AmbienteFiscal.Homologacao;
    public string RegimeTributario { get; set; } = "SimplesNacional";

    public int SerieNfce { get; set; } = 1;
    public int SerieNfe { get; set; } = 1;
    public int SerieNfse { get; set; } = 1;

    public long ProximoNumeroNfce { get; set; } = 1;
    public long ProximoNumeroNfe { get; set; } = 1;
    public long ProximoNumeroNfse { get; set; } = 1;

    public string? ProvedorFiscal { get; set; }
    public string? MunicipioCodigo { get; set; }
    public string? CnaePrincipal { get; set; }
    public string? ItemListaServico { get; set; }
    public string? CodigoTributarioMunicipio { get; set; }

    public string? NaturezaOperacaoPadrao { get; set; }
    public bool IssRetidoPadrao { get; set; }
    public decimal? AliquotaIssPadrao { get; set; }

    public bool Ativo { get; set; } = true;
}
