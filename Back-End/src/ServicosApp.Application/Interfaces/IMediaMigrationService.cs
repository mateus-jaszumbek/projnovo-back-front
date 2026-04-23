namespace ServicosApp.Application.Interfaces;

public interface IMediaMigrationService
{
    Task MigrateInlineMediaAsync(CancellationToken cancellationToken);
}
