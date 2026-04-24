namespace ServicosApp.Application.DTOs;

public class FiscalPendingSyncResultDto
{
    public bool Enabled { get; set; }
    public int ScannedCount { get; set; }
    public int ProcessedCount { get; set; }
    public int AuthorizedCount { get; set; }
    public int CancelledCount { get; set; }
    public int StillPendingCount { get; set; }
    public int FailedCount { get; set; }
}
