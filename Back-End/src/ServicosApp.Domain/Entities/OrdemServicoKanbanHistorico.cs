namespace ServicosApp.Domain.Entities;

public class OrdemServicoKanbanHistorico : EmpresaOwnedEntity
{
    public Guid OrdemServicoId { get; set; }
    public OrdemServico? OrdemServico { get; set; }

    public Guid? ColunaOrigemId { get; set; }
    public KanbanColuna? ColunaOrigem { get; set; }

    public Guid ColunaDestinoId { get; set; }
    public KanbanColuna? ColunaDestino { get; set; }

    public Guid UsuarioId { get; set; }

    public string? NomeColunaOrigem { get; set; }
    public string NomeColunaDestino { get; set; } = string.Empty;

    public bool HistoricoPublico { get; set; }
    public string? TituloPublico { get; set; }
    public string? DescricaoPublica { get; set; }

    public string PublicTrackingToken { get; set; } = string.Empty;
    public DateTime DataMovimentacao { get; set; }
}