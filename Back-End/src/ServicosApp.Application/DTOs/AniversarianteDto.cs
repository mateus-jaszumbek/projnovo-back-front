namespace ServicosApp.Application.DTOs;

public class AniversarianteDto
{
    public Guid ClienteId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public DateOnly DataAniversario { get; set; }
    public int Dia { get; set; }
    public int Mes { get; set; }
}
