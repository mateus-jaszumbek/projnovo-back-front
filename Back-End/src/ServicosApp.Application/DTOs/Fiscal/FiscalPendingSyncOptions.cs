namespace ServicosApp.Application.DTOs.Fiscal;

public class FiscalPendingSyncOptions
{
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 120;
    public int CooldownSeconds { get; set; } = 45;
    public int BatchSize { get; set; } = 20;
    public int StartupDelaySeconds { get; set; } = 20;
}
