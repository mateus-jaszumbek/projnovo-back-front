namespace ServicosApp.Application.DTOs;

public class ClienteInativoDto
{
    public Guid ClienteId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public DateTime UltimaVisita { get; set; }
    public int DiasSemContato { get; set; }
}
