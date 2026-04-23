using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServicosApp.Application.Interfaces;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public sealed class MediaMigrationService : IMediaMigrationService
{
    private readonly AppDbContext _context;
    private readonly IMediaStorageService _mediaStorageService;
    private readonly ILogger<MediaMigrationService> _logger;

    public MediaMigrationService(
        AppDbContext context,
        IMediaStorageService mediaStorageService,
        ILogger<MediaMigrationService> logger)
    {
        _context = context;
        _mediaStorageService = mediaStorageService;
        _logger = logger;
    }

    public async Task MigrateInlineMediaAsync(CancellationToken cancellationToken)
    {
        var migratedLogos = 0;
        var migratedPhotos = 0;

        var empresas = await _context.Empresas
            .Where(x => x.LogoUrl != null && x.LogoUrl.StartsWith("data:"))
            .ToListAsync(cancellationToken);

        foreach (var empresa in empresas)
        {
            if (!InlineMediaHelper.TryParseDataUrl(empresa.LogoUrl, out var inlineLogo) || inlineLogo is null)
                continue;

            await using var logoStream = new MemoryStream(inlineLogo.Bytes);
            var extension = InlineMediaHelper.ResolveExtension("logo", inlineLogo.ContentType);
            var storedLogo = await _mediaStorageService.SaveAsync(
                $"{empresa.Id}/logo/{Guid.NewGuid():N}{extension}",
                $"logo{extension}",
                inlineLogo.ContentType,
                logoStream,
                cancellationToken);

            empresa.LogoUrl = storedLogo.PublicUrl;
            migratedLogos++;
        }

        var ordens = await _context.OrdensServico
            .Where(x => x.FotosJson != null && x.FotosJson.Contains("data:"))
            .ToListAsync(cancellationToken);

        foreach (var ordem in ordens)
        {
            var fotos = OrdemServicoFotoJson.Parse(ordem.FotosJson);
            var changed = false;

            foreach (var foto in fotos)
            {
                if (!InlineMediaHelper.TryParseDataUrl(foto.DataUrl, out var inlinePhoto) || inlinePhoto is null)
                    continue;

                var fileName = string.IsNullOrWhiteSpace(foto.NomeArquivo)
                    ? $"foto-{foto.Id:N}"
                    : foto.NomeArquivo;
                var extension = InlineMediaHelper.ResolveExtension(fileName, inlinePhoto.ContentType);
                var photoId = foto.Id == Guid.Empty ? Guid.NewGuid() : foto.Id;

                await using var photoStream = new MemoryStream(inlinePhoto.Bytes);
                var storedPhoto = await _mediaStorageService.SaveAsync(
                    $"{ordem.EmpresaId}/ordens-servico/{ordem.Id}/fotos/{photoId:N}{extension}",
                    fileName,
                    inlinePhoto.ContentType,
                    photoStream,
                    cancellationToken);

                foto.Id = photoId;
                foto.ContentType = inlinePhoto.ContentType;
                foto.TamanhoBytes = inlinePhoto.Bytes.LongLength;
                foto.DataUrl = storedPhoto.PublicUrl;
                changed = true;
                migratedPhotos++;
            }

            if (!changed)
                continue;

            ordem.FotosJson = OrdemServicoFotoJson.Serialize(fotos);
        }

        if (migratedLogos == 0 && migratedPhotos == 0)
            return;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Midias inline migradas com sucesso. Logos: {LogoCount}, Fotos: {PhotoCount}",
            migratedLogos,
            migratedPhotos);
    }
}
