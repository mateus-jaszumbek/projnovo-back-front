namespace ServicosApp.Domain.Entities;

public class Tecnico : EmpresaOwnedEntity
{

    public string Nome { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Especialidade { get; set; }
    public string? Observacoes { get; set; }
    public bool Ativo { get; set; } = true;

    public List<OrdemServico> OrdensServico { get; set; } = new();
}