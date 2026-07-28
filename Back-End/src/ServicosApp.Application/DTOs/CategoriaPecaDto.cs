namespace ServicosApp.Application.DTOs;

public class CategoriaPecaDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
