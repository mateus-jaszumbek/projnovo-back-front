using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IKanbanService
{
    Task<List<KanbanPublicoColunaDto>> ObterQuadroPublicoAsync(Guid empresaId, CancellationToken cancellationToken = default);
    Task<List<KanbanPublicoCardDto>> ListarEncerradosPublicoAsync(Guid empresaId, CancellationToken cancellationToken = default);

    Task<List<KanbanConfiguracaoColunaDto>> ObterConfiguracaoPublicaAsync(Guid empresaId, CancellationToken cancellationToken = default);
    Task<KanbanConfiguracaoColunaDto> CriarColunaPublicaAsync(Guid empresaId, CreateKanbanPublicoColunaDto dto, CancellationToken cancellationToken = default);
    Task<KanbanConfiguracaoColunaDto?> AtualizarColunaPublicaAsync(Guid empresaId, Guid colunaId, UpdateKanbanPublicoColunaDto dto, CancellationToken cancellationToken = default);
    Task<bool> ReordenarColunaPublicaAsync(Guid empresaId, Guid colunaId, ReordenarKanbanColunaDto dto, CancellationToken cancellationToken = default);
    Task<bool> ExcluirColunaPublicaAsync(Guid empresaId, Guid colunaId, CancellationToken cancellationToken = default);

    Task<KanbanPublicoCardDto?> MoverCardPublicoAsync(Guid empresaId, Guid ordemServicoId, MoveKanbanPublicoCardDto dto, Guid usuarioId, CancellationToken cancellationToken = default);
    Task<KanbanPublicoCardDto?> ReabrirCardPublicoAsync(Guid empresaId, Guid ordemServicoId, Guid usuarioId, CancellationToken cancellationToken = default);

    Task<List<KanbanPrivadoColunaDto>> ObterMeuKanbanPrivadoAsync(Guid empresaId, Guid usuarioId, CancellationToken cancellationToken = default);
    Task<KanbanPrivadoColunaDto> CriarColunaPrivadaAsync(Guid empresaId, Guid usuarioId, CreateKanbanPrivadoColunaDto dto, CancellationToken cancellationToken = default);
    Task<KanbanPrivadoColunaDto?> AtualizarColunaPrivadaAsync(Guid empresaId, Guid usuarioId, Guid colunaId, UpdateKanbanPrivadoColunaDto dto, CancellationToken cancellationToken = default);
    Task<bool> ExcluirColunaPrivadaAsync(Guid empresaId, Guid usuarioId, Guid colunaId, CancellationToken cancellationToken = default);

    Task<KanbanPrivadoCardDto> CriarTarefaPrivadaAsync(Guid empresaId, Guid usuarioId, CreateKanbanTarefaPrivadaDto dto, CancellationToken cancellationToken = default);
    Task<KanbanPrivadoCardDto?> AtualizarTarefaPrivadaAsync(Guid empresaId, Guid usuarioId, Guid tarefaId, UpdateKanbanTarefaPrivadaDto dto, CancellationToken cancellationToken = default);
    Task<KanbanPrivadoCardDto?> MoverTarefaPrivadaAsync(Guid empresaId, Guid usuarioId, Guid tarefaId, MoveKanbanTarefaPrivadaDto dto, CancellationToken cancellationToken = default);
    Task<bool> ExcluirTarefaPrivadaAsync(Guid empresaId, Guid usuarioId, Guid tarefaId, CancellationToken cancellationToken = default);

    Task<KanbanTrackingPublicoDto?> ObterTrackingPublicoAsync(string token, CancellationToken cancellationToken = default);
}