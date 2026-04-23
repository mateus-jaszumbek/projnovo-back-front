namespace ServicosApp.Domain.Entities;

public class OrdemServico : EmpresaOwnedEntity
{
    public long NumeroOs { get; set; }
    public Guid? DocumentoFiscalId { get; set; }
    public DocumentoFiscal? DocumentoFiscal { get; set; }

    public Guid ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public Guid AparelhoId { get; set; }
    public Aparelho? Aparelho { get; set; }

    public Guid? TecnicoId { get; set; }
    public Tecnico? Tecnico { get; set; }

    public string Status { get; set; } = "ABERTA"; 

    public string DefeitoRelatado { get; set; } = string.Empty;
    public string? Diagnostico { get; set; }
    public string? LaudoTecnico { get; set; }

    public decimal ValorMaoObra { get; set; }
    public decimal ValorPecas { get; set; }
    public decimal Desconto { get; set; }
    public decimal ValorTotal { get; set; }

    public DateTime DataEntrada { get; set; } = DateTime.UtcNow;
    public DateTime? DataPrevisao { get; set; }
    public DateTime? DataAprovacao { get; set; }
    public DateTime? DataConclusao { get; set; }
    public DateTime? DataEntrega { get; set; }

    public int GarantiaDias { get; set; }

    public string? ObservacoesInternas { get; set; }
    public string? ObservacoesCliente { get; set; }
    public string? FotosJson { get; set; }

    public Guid? CreatedBy { get; set; }
    public Usuario? UsuarioCriacao { get; set; }

    public Guid? UpdatedBy { get; set; }
    public Usuario? UsuarioAtualizacao { get; set; }

    public List<OrdemServicoItem> Itens { get; set; } = new();

    public Guid? KanbanColunaAtualId { get; set; }
    public string TrackingToken { get; set; } = Guid.NewGuid().ToString("N");
    public bool TrackingPublicoAtivo { get; set; } = true;
}
