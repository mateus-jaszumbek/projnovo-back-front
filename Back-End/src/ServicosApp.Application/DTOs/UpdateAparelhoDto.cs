namespace ServicosApp.Application.DTOs;

public class UpdateAparelhoDto
{
    public Guid ClienteId { get; set; }

    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string? Cor { get; set; }
    public string? Imei { get; set; }
    public string? SerialNumber { get; set; }
    public string? SenhaAparelho { get; set; }
    public string? Acessorios { get; set; }
    public string? EstadoFisico { get; set; }
    public string? Observacoes { get; set; }
    public bool Ativo { get; set; } = true;
}