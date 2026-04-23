using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private readonly IMediaStorageService _mediaStorageService;

    public MediaController(IMediaStorageService mediaStorageService)
    {
        _mediaStorageService = mediaStorageService;
    }

    [HttpGet("{**storageKey}")]
    public async Task<IActionResult> Obter(string storageKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return NotFound();

        await using var file = await _mediaStorageService.OpenReadAsync(storageKey, cancellationToken);
        if (file is null)
            return NotFound();

        using var memory = new MemoryStream();
        await file.Content.CopyToAsync(memory, cancellationToken);

        Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        return File(memory.ToArray(), file.ContentType);
    }
}
