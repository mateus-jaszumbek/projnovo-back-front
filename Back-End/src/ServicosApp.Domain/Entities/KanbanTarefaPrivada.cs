namespace ServicosApp.Domain.Entities;

public class KanbanTarefaPrivada : EmpresaOwnedEntity
{
    public Guid UsuarioId { get; set; }

    public Guid KanbanColunaId { get; set; }
    public KanbanColuna? KanbanColuna { get; set; }

    public Guid? OrdemServicoId { get; set; }
    public OrdemServico? OrdemServico { get; set; }

    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int Ordem { get; set; }
    public bool Ativo { get; set; } = true;
}