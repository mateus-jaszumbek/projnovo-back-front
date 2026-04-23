namespace ServicosApp.Application.DTOs;

public class ImeiLookupDto
{
    public string Imei { get; set; } = string.Empty;
    public string Tac { get; set; } = string.Empty;
    public bool Valido { get; set; }
    public bool Encontrado { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? NomeComercial { get; set; }
    public string? Cor { get; set; }
    public string? Capacidade { get; set; }
    public string Fonte { get; set; } = "local";
    public string? Mensagem { get; set; }
}
