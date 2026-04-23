namespace ServicosApp.Application.DTOs;

public class ConfiguracaoFiscalDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public string Ambiente { get; set; } = string.Empty;
    public string RegimeTributario { get; set; } = string.Empty;

    public int SerieNfce { get; set; }
    public int SerieNfe { get; set; }
    public int SerieNfse { get; set; }

    public long ProximoNumeroNfce { get; set; }
    public long ProximoNumeroNfe { get; set; }
    public long ProximoNumeroNfse { get; set; }

    public string? ProvedorFiscal { get; set; }
    public string? MunicipioCodigo { get; set; }
    public string? CnaePrincipal { get; set; }
    public string? ItemListaServico { get; set; }

    public string? NaturezaOperacaoPadrao { get; set; }
    public bool IssRetidoPadrao { get; set; }
    public decimal? AliquotaIssPadrao { get; set; }

    public bool Ativo { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}