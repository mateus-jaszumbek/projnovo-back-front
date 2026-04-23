using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Infrastructure.Data;
using ServicosApp.Infrastructure.Services;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/ordens-servico/{ordemServicoId:guid}/fotos")]
public class OrdensServicoFotosController : ApiTenantControllerBase
{
    private const long MaxPhotoBytes = 2 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly AppDbContext _context;
    private readonly IMediaStorageService _mediaStorageService;
    private readonly IRemoteImageFetchService _remoteImageFetchService;

    public OrdensServicoFotosController(
        AppDbContext context,
        IMediaStorageService mediaStorageService,
        IRemoteImageFetchService remoteImageFetchService)
    {
        _context = context;
        _mediaStorageService = mediaStorageService;
        _remoteImageFetchService = remoteImageFetchService;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrdemServicoFotoDto>>> Listar(
        Guid ordemServicoId,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var fotosJson = await _context.OrdensServico
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Id == ordemServicoId)
            .Select(x => x.FotosJson)
            .FirstOrDefaultAsync(cancellationToken);

        if (fotosJson is null)
            return NotFound(new { message = "OS nao encontrada." });

        return Ok(OrdemServicoFotoJson.Parse(fotosJson));
    }

    [HttpPost]
    [RequestSizeLimit(MaxPhotoBytes + 256_000)]
    public async Task<ActionResult<List<OrdemServicoFotoDto>>> Enviar(
        Guid ordemServicoId,
        [FromForm] IFormFile? arquivo,
        [FromForm] string? url,
        [FromForm] string? descricao,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var ordem = await _context.OrdensServico
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == ordemServicoId, cancellationToken);

        if (ordem is null)
            return NotFound(new { message = "OS nao encontrada." });

        var fotos = OrdemServicoFotoJson.Parse(ordem.FotosJson);
        if (fotos.Count >= 12)
            return BadRequest(new { message = "Limite de 12 fotos por OS atingido." });

        if ((arquivo is null || arquivo.Length == 0) && string.IsNullOrWhiteSpace(url))
            return BadRequest(new { message = "Selecione uma foto ou informe uma URL." });

        string fileName;
        string contentType;
        long contentLength;
        Stream stream;

        try
        {
            if (arquivo is not null && arquivo.Length > 0)
            {
                if (arquivo.Length > MaxPhotoBytes)
                    return BadRequest(new { message = "A foto deve ter no maximo 2 MB." });

                if (!AllowedContentTypes.Contains(arquivo.ContentType))
                    return BadRequest(new { message = "Envie imagens JPG, PNG ou WebP." });

                fileName = arquivo.FileName;
                contentType = arquivo.ContentType;
                contentLength = arquivo.Length;
                stream = arquivo.OpenReadStream();
            }
            else
            {
                var remoteImage = await _remoteImageFetchService.DownloadAsync(
                    url ?? string.Empty,
                    MaxPhotoBytes,
                    AllowedContentTypes.ToArray(),
                    cancellationToken);

                fileName = remoteImage.FileName;
                contentType = remoteImage.ContentType;
                contentLength = remoteImage.Bytes.LongLength;
                stream = new MemoryStream(remoteImage.Bytes, writable: false);
            }
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var photoId = Guid.NewGuid();
        var extension = InlineMediaHelper.ResolveExtension(fileName, contentType);
        await using var uploadStream = stream;
        var storedPhoto = await _mediaStorageService.SaveAsync(
            $"{empresaId}/ordens-servico/{ordemServicoId}/fotos/{photoId:N}{extension}",
            fileName,
            contentType,
            uploadStream,
            cancellationToken);

        var foto = new OrdemServicoFotoDto
        {
            Id = photoId,
            NomeArquivo = Path.GetFileName(fileName),
            ContentType = contentType,
            TamanhoBytes = contentLength,
            Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim(),
            DataUrl = storedPhoto.PublicUrl,
            CreatedAt = DateTime.UtcNow
        };

        fotos.Add(foto);
        ordem.FotosJson = OrdemServicoFotoJson.Serialize(fotos);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _mediaStorageService.DeleteAsync(storedPhoto.PublicUrl, cancellationToken);
            throw;
        }

        return Ok(fotos);
    }

    [HttpDelete("{fotoId:guid}")]
    public async Task<IActionResult> Excluir(
        Guid ordemServicoId,
        Guid fotoId,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var ordem = await _context.OrdensServico
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == ordemServicoId, cancellationToken);

        if (ordem is null)
            return NotFound(new { message = "OS nao encontrada." });

        var fotos = OrdemServicoFotoJson.Parse(ordem.FotosJson);
        var fotoParaExcluir = fotos.FirstOrDefault(x => x.Id == fotoId);
        var removidas = fotos.RemoveAll(x => x.Id == fotoId);

        if (removidas == 0)
            return NotFound(new { message = "Foto nao encontrada." });

        ordem.FotosJson = OrdemServicoFotoJson.Serialize(fotos);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _mediaStorageService.DeleteAsync(fotoParaExcluir?.DataUrl, cancellationToken);
        }
        catch
        {
        }

        return NoContent();
    }
}
