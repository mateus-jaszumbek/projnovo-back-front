namespace ServicosApp.Application.DTOs;

public class TecnicoDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Especialidade { get; set; }
    public string? Observacoes { get; set; }
    public bool Ativo { get; set; }
}