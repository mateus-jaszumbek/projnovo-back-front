namespace ServicosApp.Application.DTOs;

public class FiscalChecklistItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string Status { get; set; } = "info";
    public string Detail { get; set; } = string.Empty;
    public bool BlocksHomologacao { get; set; }
    public bool BlocksProducao { get; set; }
}
