using ServicosApp.Domain.Entities;

namespace ServicosApp.Domain.Entities;

public class KanbanFluxo : EmpresaOwnedEntity
{
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = "PUBLICO"; // PUBLICO | PRIVADO
    public Guid? UsuarioId { get; set; }
    public bool Ativo { get; set; } = true;

    public List<KanbanColuna> Colunas { get; set; } = new();
}