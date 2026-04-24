namespace ServicosApp.Application.DTOs;

public class FocusNfseMunicipioValidacaoDto
{
    public string ProviderCode { get; set; } = string.Empty;
    public string? MunicipioCodigo { get; set; }
    public string? MunicipioNome { get; set; }
    public string? Uf { get; set; }
    public string? StatusNfse { get; set; }

    public bool RemoteValidationAvailable { get; set; }
    public bool PodeEmitirNfse { get; set; }
    public bool ItemListaServicoConfigurado { get; set; }
    public bool CnaePrincipalConfigurado { get; set; }
    public bool CodigoTributarioMunicipioConfigurado { get; set; }
    public bool? CodigoTributarioMunicipioObrigatorio { get; set; }

    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
