using ServicosApp.Application.DTOs.Fornecedores;

namespace ServicosApp.Application.Interfaces;

public interface IFornecedorService
{
    Task<FornecedorDto> CriarAsync(Guid empresaId, CreateFornecedorDto dto, CancellationToken cancellationToken = default);
    Task<List<FornecedorDto>> ListarAsync(Guid empresaId, CancellationToken cancellationToken = default);
    Task<FornecedorDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
    Task<FornecedorDto?> AtualizarAsync(Guid empresaId, Guid id, UpdateFornecedorDto dto, CancellationToken cancellationToken = default);
    Task<bool> InativarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
    Task<FornecedorMensagemHistoricoDto> RegistrarMensagemAsync(Guid empresaId, Guid fornecedorId, CreateFornecedorMensagemHistoricoDto dto, CancellationToken cancellationToken = default);
    Task<List<FornecedorMensagemHistoricoDto>> ListarMensagensAsync(Guid empresaId, Guid fornecedorId, CancellationToken cancellationToken = default);
}
