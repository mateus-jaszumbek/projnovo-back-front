namespace ServicosApp.Application.DTOs;

public class OrdemServicoFotoDto
{
    public Guid Id { get; set; }
    public string NomeArquivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long TamanhoBytes { get; set; }
    public string? Descricao { get; set; }
    public string DataUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
