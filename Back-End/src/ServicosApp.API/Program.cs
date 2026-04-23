using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ServicosApp.API;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Application.Interfaces;
using ServicosApp.Infrastructure.Data;
using ServicosApp.Infrastructure.PostgresMigrations;
using ServicosApp.Infrastructure.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problem = new ValidationProblemDetails(context.ModelState)
            {
                Title = "Erro de validaï¿½ï¿½o",
                Detail = "Um ou mais campos estï¿½o invï¿½lidos.",
                Status = StatusCodes.Status400BadRequest,
                Instance = context.HttpContext.Request.Path
            };

            problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            return new BadRequestObjectResult(problem);
        };
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
    var configuredOrigins = builder.Configuration
        .GetSection("Security:AllowedCorsOrigins")
        .Get<string[]>()?
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Select(origin => origin.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray() ?? [];

    var allowedOrigins = configuredOrigins.Length > 0
        ? configuredOrigins
        : builder.Environment.IsDevelopment()
            ? ["http://localhost:5173", "http://127.0.0.1:5173"]
            : [];

    options.AddPolicy("Frontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.Configure<MediaStorageOptions>(builder.Configuration.GetSection("MediaStorage"));
builder.Services.Configure<ImeiLookupOptions>(builder.Configuration.GetSection("ImeiLookup"));

var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' nï¿½o configurada.");

var configuredDatabaseProvider = builder.Configuration["Database:Provider"];
var databaseProvider = string.IsNullOrWhiteSpace(configuredDatabaseProvider)
    ? InferDatabaseProvider(rawConnectionString)
    : configuredDatabaseProvider.Trim();

var usePostgreSql = databaseProvider.Equals("postgres", StringComparison.OrdinalIgnoreCase)
    || databaseProvider.Equals("postgresql", StringComparison.OrdinalIgnoreCase);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (usePostgreSql)
    {
        options.UseNpgsql(
            rawConnectionString,
            npgsql => npgsql.MigrationsAssembly(typeof(PostgresMigrationsMarker).Assembly.FullName));
        return;
    }

    var sqliteConnectionString = new SqliteConnectionStringBuilder(rawConnectionString)
    {
        DefaultTimeout = 60
    }.ToString();

    options.UseSqlite(sqliteConnectionString);
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key nï¿½o configurado.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key deve ter pelo menos 32 bytes.");

if (!builder.Environment.IsDevelopment() &&
    jwtKey == "12345678910111213141516171819202123242526272829303132")
{
    throw new InvalidOperationException("Configure uma Jwt:Key segura fora do appsettings antes de publicar.");
}

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });

    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("OwnerOuSuperAdmin", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "isSuperAdmin" && c.Value == "true") ||
            context.User.HasClaim(c => c.Type == "perfil" && c.Value == "owner") ||
            context.User.HasClaim(c => c.Type == "perfil" && c.Value == "admin") ||
            context.User.HasClaim(c => c.Type == "perfil" && c.Value == "administrador")));

    for (var level = 1; level <= 5; level++)
    {
        var requiredLevel = level;
        options.AddPolicy($"Nivel{requiredLevel}", policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => c.Type == "isSuperAdmin" && c.Value == "true") ||
                context.User.HasClaim(c => c.Type == "perfil" && c.Value is "owner" or "admin" or "administrador") ||
                (int.TryParse(context.User.FindFirst("nivelAcesso")?.Value, out var nivelAcesso) &&
                 nivelAcesso >= requiredLevel)));
    }
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IMediaStorageService, MediaStorageService>();
builder.Services.AddScoped<IMediaMigrationService, MediaMigrationService>();
builder.Services.AddHttpClient<IRemoteImageFetchService, RemoteImageFetchService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddHttpClient<ITacCacheBootstrapService, TacCacheBootstrapService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddHttpClient<IImeiLookupService, ImeiLookupService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(12);
});

builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IFornecedorService, FornecedorService>();
builder.Services.AddScoped<IUsuarioEmpresaService, UsuarioEmpresaService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IAparelhoService, AparelhoService>();
builder.Services.AddScoped<ITecnicoService, TecnicoService>();
builder.Services.AddScoped<IServicoCatalogoService, ServicoCatalogoService>();
builder.Services.AddScoped<IPecaService, PecaService>();
builder.Services.AddScoped<IOrdemServicoService, OrdemServicoService>();
builder.Services.AddScoped<IOrdemServicoItemService, OrdemServicoItemService>();
builder.Services.AddScoped<IEstoqueMovimentoService, EstoqueMovimentoService>();
builder.Services.AddScoped<IVendaService, VendaService>();
builder.Services.AddScoped<IVendaItemService, VendaItemService>();
builder.Services.AddScoped<ICaixaDiarioService, CaixaDiarioService>();
builder.Services.AddScoped<ICaixaLancamentoService, CaixaLancamentoService>();
builder.Services.AddScoped<IContaReceberService, ContaReceberService>();
builder.Services.AddScoped<IContaPagarService, ContaPagarService>();
builder.Services.AddScoped<IModuloPersonalizadoService, ModuloPersonalizadoService>();
builder.Services.AddScoped<IKanbanService, KanbanService>();
builder.Services.AddScoped<IGestaoService, GestaoService>();
builder.Services.AddScoped<IConfiguracaoFiscalService, ConfiguracaoFiscalService>();
builder.Services.AddScoped<INumeracaoFiscalService, NumeracaoFiscalService>();
builder.Services.AddScoped<IDocumentoFiscalBuilderService, DocumentoFiscalBuilderService>();
builder.Services.AddScoped<IDocumentoFiscalConsultaService, DocumentoFiscalConsultaService>();
builder.Services.AddScoped<IRegraFiscalProdutoService, RegraFiscalProdutoService>();
builder.Services.AddScoped<INfseProviderClient, NfseProviderClientFake>();
builder.Services.AddScoped<INfseService, NfseService>();
builder.Services.AddScoped<IDfeProviderClient, DfeProviderClientFake>();
builder.Services.AddScoped<IDfeVendaService, DfeVendaService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ServicosApp API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
    context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers.TryAdd(
            "Content-Security-Policy",
            "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'");
    }

    await next();
});

app.UseForwardedHeaders();

if (builder.Configuration.GetValue<bool>("Security:ForceHttps"))
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (db.Database.IsSqlite())
    {
        var connection = (SqliteConnection)db.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
            connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL;";
        command.CommandTimeout = 60;
        command.ExecuteScalar();
    }

    db.Database.Migrate();

    var mediaMigration = scope.ServiceProvider.GetRequiredService<IMediaMigrationService>();
    await mediaMigration.MigrateInlineMediaAsync(CancellationToken.None);

    var tacCacheBootstrap = scope.ServiceProvider.GetRequiredService<ITacCacheBootstrapService>();
    await tacCacheBootstrap.EnsureCacheReadyAsync(CancellationToken.None);
}

app.Run();

static string InferDatabaseProvider(string connectionString)
{
    if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
        connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase) ||
        connectionString.Contains("Port=", StringComparison.OrdinalIgnoreCase))
    {
        return "PostgreSql";
    }

    return "Sqlite";
}

