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
                Title = "Erro de validação",
                Detail = "Um ou mais campos estão inválidos.",
                Status = StatusCodes.Status400BadRequest,
                Instance = context.HttpContext.Request.Path
            };

            problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            return new BadRequestObjectResult(problem);
        };
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddDataProtection();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
    var allowedOrigins = GetAllowedCorsOrigins(
        builder.Configuration,
        builder.Environment.IsDevelopment());

    options.AddPolicy("Frontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.Configure<MediaStorageOptions>(builder.Configuration.GetSection("MediaStorage"));
builder.Services.Configure<ImeiLookupOptions>(builder.Configuration.GetSection("ImeiLookup"));
builder.Services.Configure<FiscalPendingSyncOptions>(builder.Configuration.GetSection("FiscalPendingSync"));
builder.Services.Configure<FocusWebhookOptions>(builder.Configuration.GetSection("FocusWebhook"));

var rawConnectionString = ResolveConnectionString(builder.Configuration)
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não configurada.");

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
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key não configurado.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key deve ter pelo menos 32 bytes.");

if (!builder.Environment.IsDevelopment() &&
    jwtKey == "vcbhhO5z2D+OQknzPJQgpOmtirplB23NFd4OUU0e9TAAWS7hEuAlV7qzvquiR6J28XoQh7s=")
{
    throw new InvalidOperationException("Configure uma Jwt:Key segura fora do appsettings antes de publicar.");
}

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    if (!builder.Environment.IsDevelopment())
    {
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
    }

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

        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token))
                    context.Token = context.Request.Cookies["access_token"];
                return Task.CompletedTask;
            }
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
builder.Services.AddHttpClient<IFocusWebhookRegistrationService, FocusWebhookRegistrationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddScoped<ICredencialFiscalEmpresaService, CredencialFiscalEmpresaService>();
builder.Services.AddScoped<IFiscalCredentialSecretProtector, FiscalCredentialSecretProtector>();
builder.Services.AddScoped<INumeracaoFiscalService, NumeracaoFiscalService>();
builder.Services.AddScoped<IDocumentoFiscalBuilderService, DocumentoFiscalBuilderService>();
builder.Services.AddScoped<IDocumentoFiscalConsultaService, DocumentoFiscalConsultaService>();
builder.Services.AddHttpClient<IDocumentoFiscalArquivoService, DocumentoFiscalArquivoService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddScoped<IFiscalPendingSyncService, FiscalPendingSyncService>();
builder.Services.AddScoped<IFocusFiscalWebhookService, FocusFiscalWebhookService>();
builder.Services.AddScoped<IRegraFiscalProdutoService, RegraFiscalProdutoService>();
builder.Services.AddHttpClient<FocusNfeNfseProviderClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<FocusNfeDfeProviderClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<IFocusNfseMunicipioService, FocusNfseMunicipioService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddScoped<INfseProviderClient, NfseProviderClientFake>();
builder.Services.AddScoped<INfseProviderClient>(sp => sp.GetRequiredService<FocusNfeNfseProviderClient>());
builder.Services.AddScoped<INfseProviderResolver, NfseProviderResolver>();
builder.Services.AddScoped<INfseService, NfseService>();
builder.Services.AddScoped<IDfeProviderClient, DfeProviderClientFake>();
builder.Services.AddScoped<IDfeProviderClient>(sp => sp.GetRequiredService<FocusNfeDfeProviderClient>());
builder.Services.AddScoped<IDfeProviderResolver, DfeProviderResolver>();
builder.Services.AddScoped<IDfeVendaService, DfeVendaService>();
builder.Services.AddHostedService<FiscalPendingSyncWorker>();

builder.Services.AddScoped<IPagamentoCredentialSecretProtector, PagamentoCredentialSecretProtector>();
builder.Services.AddHttpClient<MercadoPagoPagamentoProviderClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddScoped<IPagamentoProviderClient>(sp => sp.GetRequiredService<MercadoPagoPagamentoProviderClient>());
// Esqueletos aguardando credenciais reais de cada adquirente - ver comentário em cada classe.
builder.Services.AddHttpClient<StonePagamentoProviderClient>();
builder.Services.AddScoped<IPagamentoProviderClient>(sp => sp.GetRequiredService<StonePagamentoProviderClient>());
builder.Services.AddHttpClient<PagBankPagamentoProviderClient>();
builder.Services.AddScoped<IPagamentoProviderClient>(sp => sp.GetRequiredService<PagBankPagamentoProviderClient>());
builder.Services.AddHttpClient<GetNetPagamentoProviderClient>();
builder.Services.AddScoped<IPagamentoProviderClient>(sp => sp.GetRequiredService<GetNetPagamentoProviderClient>());
builder.Services.AddScoped<IPagamentoProviderResolver, PagamentoProviderResolver>();
builder.Services.AddScoped<IConfiguracaoPagamentoService, ConfiguracaoPagamentoService>();
builder.Services.AddScoped<ICobrancaPagamentoService, CobrancaPagamentoService>();

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

static string? ResolveConnectionString(IConfiguration configuration)
{
    var directCandidates = new[]
    {
        configuration.GetConnectionString("DefaultConnection"),
        configuration["DATABASE_CONNECTION_STRING"],
        configuration["DATABASE_URL"],
        configuration["DATABASE_PUBLIC_URL"],
        configuration["POSTGRES_URL"],
        configuration["POSTGRES_PUBLIC_URL"]
    };

    foreach (var candidate in directCandidates)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            continue;

        return NormalizeConnectionString(candidate);
    }

    var host = configuration["PGHOST"];
    var port = configuration["PGPORT"];
    var database = configuration["PGDATABASE"];
    var username = configuration["PGUSER"];
    var password = configuration["PGPASSWORD"];

    if (string.IsNullOrWhiteSpace(host) ||
        string.IsNullOrWhiteSpace(port) ||
        string.IsNullOrWhiteSpace(database) ||
        string.IsNullOrWhiteSpace(username) ||
        string.IsNullOrWhiteSpace(password))
    {
        return null;
    }

    return string.Join(
        ';',
        $"Host={host.Trim()}",
        $"Port={port.Trim()}",
        $"Database={database.Trim()}",
        $"Username={username.Trim()}",
        $"Password={password.Trim()}");
}

static string NormalizeConnectionString(string rawValue)
{
    var trimmedValue = rawValue.Trim();
    if (trimmedValue.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        trimmedValue.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return ConvertPostgresUrlToConnectionString(trimmedValue);
    }

    return trimmedValue;
}

static string ConvertPostgresUrlToConnectionString(string postgresUrl)
{
    var normalizedUrl = postgresUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        ? "postgresql://" + postgresUrl["postgres://".Length..]
        : postgresUrl;

    var uri = new Uri(normalizedUrl);
    var userInfoParts = uri.UserInfo.Split(':', 2);
    var username = userInfoParts.Length > 0 ? Uri.UnescapeDataString(userInfoParts[0]) : string.Empty;
    var password = userInfoParts.Length > 1 ? Uri.UnescapeDataString(userInfoParts[1]) : string.Empty;
    var database = uri.AbsolutePath.Trim('/');

    var connectionParts = new List<string>
    {
        $"Host={uri.Host}",
        $"Port={uri.Port}",
        $"Database={database}",
        $"Username={username}",
        $"Password={password}"
    };

    if (string.IsNullOrWhiteSpace(uri.Query))
        return string.Join(';', connectionParts);

    var querySegments = uri.Query.TrimStart('?')
        .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    foreach (var segment in querySegments)
    {
        var pair = segment.Split('=', 2);
        var key = Uri.UnescapeDataString(pair[0]).Trim();
        if (string.IsNullOrWhiteSpace(key))
            continue;

        var value = pair.Length > 1 ? Uri.UnescapeDataString(pair[1]).Trim() : string.Empty;
        connectionParts.Add($"{key}={value}");
    }

    return string.Join(';', connectionParts);
}

static string[] GetAllowedCorsOrigins(IConfiguration configuration, bool isDevelopment)
{
    var sectionOrigins = configuration
        .GetSection("Security:AllowedCorsOrigins")
        .Get<string[]>() ?? [];

    var csvOrigins = (configuration["Security:AllowedCorsOriginsCsv"] ?? string.Empty)
        .Split(',', ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    var aliasOrigins = new[]
    {
        configuration["APP_URL"],
        configuration["APP_URL_1"],
        configuration["APP_URL_2"],
        configuration["APP_URL_3"]
    };

    var allowedOrigins = sectionOrigins
        .Concat(csvOrigins)
        .Concat(aliasOrigins)
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Select(origin => origin!.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (allowedOrigins.Length > 0)
        return allowedOrigins;

    return isDevelopment
        ? ["http://localhost:5173", "http://127.0.0.1:5173"]
        : [];
}

