namespace ServicosApp.Application.DTOs;

public class RegistroPersonalizadoDto
{
    public Guid Id { get; set; }
    public Guid ModuloPersonalizadoId { get; set; }
    public Guid? OrigemId { get; set; }
    public Dictionary<string, object?> Valores { get; set; } = new();
    public bool Ativo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
