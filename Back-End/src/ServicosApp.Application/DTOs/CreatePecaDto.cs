using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class CreatePecaDto
{
    [Required]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? CodigoInterno { get; set; }

    [MaxLength(80)]
    public string? Sku { get; set; }

    [MaxLength(500)]
    public string? Descricao { get; set; }

    [MaxLength(100)]
    public string? Categoria { get; set; }

    [MaxLength(100)]
    public string? Marca { get; set; }

    [MaxLength(150)]
    public string? ModeloCompativel { get; set; }

    [MaxLength(20)]
    public string? Ncm { get; set; }

    [MaxLength(20)]
    public string? Cest { get; set; }

    [MaxLength(10)]
    public string? CfopPadraoNfe { get; set; }

    [MaxLength(10)]
    public string? CfopPadraoNfce { get; set; }

    [MaxLength(10)]
    public string? CstCsosn { get; set; }

    [MaxLength(2)]
    public string? OrigemMercadoria { get; set; }

    [Required]
    [MaxLength(10)]
    public string Unidade { get; set; } = "UN";
    public Guid? FornecedorId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CustoUnitario { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PrecoVenda { get; set; }

    [Range(0, int.MaxValue)]
    public int GarantiaDias { get; set; } = 0;

    [Range(0, double.MaxValue)]
    public decimal EstoqueAtual { get; set; }

    [Range(0, double.MaxValue)]
    public decimal EstoqueMinimo { get; set; }

    public bool Ativo { get; set; } = true;
}
