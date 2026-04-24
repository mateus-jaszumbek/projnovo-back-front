namespace ServicosApp.Application.DTOs;

public class DocumentoFiscalArquivoDto
{
    public string FileName { get; set; } = "documento-fiscal.xml";
    public string ContentType { get; set; } = "application/octet-stream";
    public string Conteudo { get; set; } = string.Empty;
}
