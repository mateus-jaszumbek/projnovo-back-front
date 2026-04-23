namespace ServicosApp.Domain.Entities;

public class KanbanColuna : EmpresaOwnedEntity
{
    public Guid KanbanFluxoId { get; set; }
    public KanbanFluxo? KanbanFluxo { get; set; }

    public string NomeInterno { get; set; } = string.Empty;
    public string? NomePublico { get; set; }
    public string Cor { get; set; } = "#CBD5E1";
    public int Ordem { get; set; }
    public bool Sistema { get; set; }
    public bool Ativa { get; set; } = true;

    public bool VisivelCliente { get; set; }
    public bool GeraEventoCliente { get; set; }
    public bool EtapaFinal { get; set; }
    public string? TipoFinalizacao { get; set; } // ENTREGUE | CANCELADA
    public bool PermiteEnvioWhatsApp { get; set; }
    public string? DescricaoPublica { get; set; }

    public List<KanbanCard> Cards { get; set; } = new();
    public List<KanbanTarefaPrivada> TarefasPrivadas { get; set; } = new();
}