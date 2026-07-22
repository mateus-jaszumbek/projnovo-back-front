using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs.Pagamentos;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class ConfiguracaoPagamentoService : IConfiguracaoPagamentoService
{
    private readonly AppDbContext _context;
    private readonly IPagamentoCredentialSecretProtector _protector;
    private readonly IPagamentoProviderResolver _resolver;

    public ConfiguracaoPagamentoService(
        AppDbContext context,
        IPagamentoCredentialSecretProtector protector,
        IPagamentoProviderResolver resolver)
    {
        _context = context;
        _protector = protector;
        _resolver = resolver;
    }

    public async Task<ConfiguracaoPagamentoDto?> ObterAsync(
        Guid empresaId,
        string requestBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.ConfiguracoesPagamento
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId, cancellationToken);

        return entity is null ? null : Map(entity, requestBaseUrl);
    }

    public async Task<ConfiguracaoPagamentoDto> SalvarAsync(
        Guid empresaId,
        UpdateConfiguracaoPagamentoDto dto,
        string requestBaseUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Provider))
            throw new InvalidOperationException("Selecione um provedor de pagamento.");

        var providerCode = PagamentoProviderCodeNormalizer.Normalize(dto.Provider);
        var provedorInfo = ListarProvedoresDisponiveis().FirstOrDefault(x => x.Codigo == providerCode);

        if (provedorInfo is null)
            throw new InvalidOperationException($"Provedor de pagamento '{dto.Provider}' desconhecido.");

        if (!provedorInfo.Implementado)
            throw new InvalidOperationException($"O provedor '{provedorInfo.Nome}' ainda não está disponível. Em breve.");

        var entity = await _context.ConfiguracoesPagamento
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId, cancellationToken);

        if (entity is null)
        {
            entity = new ConfiguracaoPagamento
            {
                EmpresaId = empresaId,
                WebhookSecret = Guid.NewGuid().ToString("N")
            };

            _context.ConfiguracoesPagamento.Add(entity);
        }

        entity.Provider = providerCode;
        entity.PublicKey = string.IsNullOrWhiteSpace(dto.PublicKey) ? null : dto.PublicKey.Trim();
        entity.PosId = string.IsNullOrWhiteSpace(dto.PosId) ? null : dto.PosId.Trim();
        entity.UserIdExterno = string.IsNullOrWhiteSpace(dto.UserIdExterno) ? null : dto.UserIdExterno.Trim();
        entity.SuportaMaquininha = dto.SuportaMaquininha && provedorInfo.SuportaMaquininha;
        entity.SuportaPix = dto.SuportaPix && provedorInfo.SuportaPix;
        entity.Ativo = dto.Ativo;

        if (!string.IsNullOrWhiteSpace(dto.AccessToken))
            entity.AccessTokenEncrypted = _protector.Protect(dto.AccessToken);

        await _context.SaveChangesAsync(cancellationToken);

        return Map(entity, requestBaseUrl);
    }

    public IReadOnlyCollection<PagamentoProviderInfoDto> ListarProvedoresDisponiveis()
        => _resolver.ListarDisponiveis();

    private static ConfiguracaoPagamentoDto Map(ConfiguracaoPagamento entity, string requestBaseUrl)
    {
        return new ConfiguracaoPagamentoDto
        {
            Id = entity.Id,
            EmpresaId = entity.EmpresaId,
            Provider = entity.Provider,
            PublicKey = entity.PublicKey,
            PosId = entity.PosId,
            UserIdExterno = entity.UserIdExterno,
            AccessTokenConfigurado = !string.IsNullOrWhiteSpace(entity.AccessTokenEncrypted),
            SuportaMaquininha = entity.SuportaMaquininha,
            SuportaPix = entity.SuportaPix,
            Ativo = entity.Ativo,
            WebhookUrl = $"{requestBaseUrl.TrimEnd('/')}/api/public/pagamentos/webhooks/{entity.Provider}/{entity.WebhookSecret}",
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
