namespace ServicosApp.Domain.Entities;

public class KanbanCard : EmpresaOwnedEntity
{
    public Guid KanbanColunaId { get; set; }
    public KanbanColuna? KanbanColuna { get; set; }

    public Guid OrdemServicoId { get; set; }
    public OrdemServico? OrdemServico { get; set; }

    public string PublicTrackingToken { get; set; } = Guid.NewGuid().ToString("N");
    public bool PublicTrackingAtivo { get; set; } = true;

    public int Ordem { get; set; }
    public bool Ativo { get; set; } = true;

    public DateTime? DataEntradaColunaAtual { get; set; }
    public bool OcultoDoQuadro { get; set; }
    public DateTime? DataOcultado { get; set; }
}