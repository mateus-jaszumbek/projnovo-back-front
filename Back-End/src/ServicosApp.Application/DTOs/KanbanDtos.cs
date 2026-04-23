using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class KanbanPublicoColunaDto
{
    public Guid Id { get; set; }
    public string NomeInterno { get; set; } = string.Empty;
    public string? NomePublico { get; set; }
    public string Cor { get; set; } = "#CBD5E1";
    public int Ordem { get; set; }
    public bool Sistema { get; set; }
    public bool Ativa { get; set; }
    public bool VisivelCliente { get; set; }
    public bool GeraEventoCliente { get; set; }
    public bool EtapaFinal { get; set; }
    public bool PermiteEnvioWhatsApp { get; set; }
    public string? DescricaoPublica { get; set; }
    public List<KanbanPublicoCardDto> Cards { get; set; } = new();
}

public class KanbanPublicoCardDto
{
    public Guid Id { get; set; }
    public Guid OrdemServicoId { get; set; }
    public Guid KanbanColunaId { get; set; }
    public string PublicTrackingToken { get; set; } = string.Empty;
    public string NumeroOs { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string? TelefoneCliente { get; set; }
    public string Aparelho { get; set; } = string.Empty;
    public string? Defeito { get; set; }
    public string? Tecnico { get; set; }
    public decimal? ValorTotal { get; set; }
    public string? StatusFinanceiro { get; set; }
    public string? StatusPeca { get; set; }
    public bool Atrasada { get; set; }
    public int Ordem { get; set; }
}

public class KanbanConfiguracaoColunaDto
{
    public Guid Id { get; set; }
    public string NomeInterno { get; set; } = string.Empty;
    public string? NomePublico { get; set; }
    public string Cor { get; set; } = "#CBD5E1";
    public int Ordem { get; set; }
    public bool Sistema { get; set; }
    public bool Ativa { get; set; }
    public bool VisivelCliente { get; set; }
    public bool GeraEventoCliente { get; set; }
    public bool EtapaFinal { get; set; }
    public string? TipoFinalizacao { get; set; }
    public bool PermiteEnvioWhatsApp { get; set; }
    public string? DescricaoPublica { get; set; }
}

public class CreateKanbanPublicoColunaDto
{
    [Required]
    [MaxLength(100)]
    public string NomeInterno { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? NomePublico { get; set; }

    [MaxLength(20)]
    public string? Cor { get; set; }

    public bool VisivelCliente { get; set; }
    public bool GeraEventoCliente { get; set; }
    public bool EtapaFinal { get; set; }

    [MaxLength(20)]
    public string? TipoFinalizacao { get; set; }

    public bool PermiteEnvioWhatsApp { get; set; }

    [MaxLength(500)]
    public string? DescricaoPublica { get; set; }
}

public class UpdateKanbanPublicoColunaDto
{
    [Required]
    [MaxLength(100)]
    public string NomeInterno { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? NomePublico { get; set; }

    [MaxLength(20)]
    public string? Cor { get; set; }

    public bool Ativa { get; set; }
    public bool VisivelCliente { get; set; }
    public bool GeraEventoCliente { get; set; }
    public bool EtapaFinal { get; set; }

    [MaxLength(20)]
    public string? TipoFinalizacao { get; set; }

    public bool PermiteEnvioWhatsApp { get; set; }

    [MaxLength(500)]
    public string? DescricaoPublica { get; set; }
}

public class ReordenarKanbanColunaDto
{
    public int Ordem { get; set; }
}

public class MoveKanbanPublicoCardDto
{
    public Guid ColunaId { get; set; }
    public int Ordem { get; set; }
}

public class KanbanPrivadoColunaDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public bool Sistema { get; set; }
    public bool Ativa { get; set; }
    public List<KanbanPrivadoCardDto> Cards { get; set; } = new();
}

public class KanbanPrivadoCardDto
{
    public Guid Id { get; set; }
    public Guid KanbanColunaId { get; set; }
    public Guid? OrdemServicoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int Ordem { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateKanbanPrivadoColunaDto
{
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;
}

public class UpdateKanbanPrivadoColunaDto
{
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    public bool Ativa { get; set; } = true;
}

public class CreateKanbanTarefaPrivadaDto
{
    public Guid? KanbanColunaId { get; set; }
    public Guid? OrdemServicoId { get; set; }

    [Required]
    [MaxLength(160)]
    public string Titulo { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Descricao { get; set; }
}

public class UpdateKanbanTarefaPrivadaDto
{
    public Guid? OrdemServicoId { get; set; }

    [Required]
    [MaxLength(160)]
    public string Titulo { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Descricao { get; set; }
}

public class MoveKanbanTarefaPrivadaDto
{
    public Guid ColunaId { get; set; }
    public int Ordem { get; set; }
}

public class KanbanTrackingEtapaDto
{
    public Guid ColunaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cor { get; set; } = "#CBD5E1";
    public int Ordem { get; set; }
    public bool Atual { get; set; }
    public bool Concluida { get; set; }
}

public class KanbanTrackingEventoDto
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime Data { get; set; }
}

public class KanbanTrackingItemDto
{
    public string TipoItem { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal ValorTotal { get; set; }
}

public class KanbanTrackingPublicoDto
{
    public string PublicTrackingToken { get; set; } = string.Empty;
    public string NumeroOs { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string Aparelho { get; set; } = string.Empty;
    public string? Defeito { get; set; }
    public string? EmpresaLogoUrl { get; set; }
    public string StatusAtual { get; set; } = string.Empty;
    public Guid? ColunaAtualId { get; set; }
    public decimal? ValorTotal { get; set; }
    public List<KanbanTrackingEtapaDto> Etapas { get; set; } = new();
    public List<KanbanTrackingEventoDto> Historico { get; set; } = new();
    public List<KanbanTrackingItemDto> Itens { get; set; } = new();
    public List<OrdemServicoFotoDto> Fotos { get; set; } = new();
}
