using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Data;
using System.Data;

namespace ServicosApp.Infrastructure.Services;

public class NumeracaoFiscalService : INumeracaoFiscalService
{
    private readonly AppDbContext _context;

    public NumeracaoFiscalService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(int serie, long numero, string? serieRps, long? numeroRps)> ReservarNumeracaoNfseAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        var (serie, numero) = await ReservarNumeracaoInternaAsync(
            empresaId,
            "SerieNfse",
            "ProximoNumeroNfse",
            cancellationToken);

        return (serie, numero, serie.ToString(), numero);
    }

    public Task<(int serie, long numero)> ReservarNumeracaoAsync(
        Guid empresaId,
        TipoDocumentoFiscal tipoDocumento,
        CancellationToken cancellationToken = default)
    {
        return tipoDocumento switch
        {
            TipoDocumentoFiscal.Nfse => ReservarNumeracaoInternaAsync(
                empresaId,
                "SerieNfse",
                "ProximoNumeroNfse",
                cancellationToken),

            TipoDocumentoFiscal.Nfe => ReservarNumeracaoInternaAsync(
                empresaId,
                "SerieNfe",
                "ProximoNumeroNfe",
                cancellationToken),

            TipoDocumentoFiscal.Nfce => ReservarNumeracaoInternaAsync(
                empresaId,
                "SerieNfce",
                "ProximoNumeroNfce",
                cancellationToken),

            _ => throw new InvalidOperationException("Tipo de documento fiscal inválido.")
        };
    }

    private async Task<(int serie, long numero)> ReservarNumeracaoInternaAsync(
        Guid empresaId,
        string serieColumn,
        string numeroColumn,
        CancellationToken cancellationToken)
    {
        const int maxTentativas = 5;

        for (var tentativa = 1; tentativa <= maxTentativas; tentativa++)
        {
            try
            {
                var connection = _context.Database.GetDbConnection();

                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync(cancellationToken);

                await using var command = connection.CreateCommand();
                command.CommandText = $"""
                    UPDATE "configuracoes_fiscais"
                    SET "{numeroColumn}" = "{numeroColumn}" + 1,
                        "UpdatedAt" = $updatedAt
                    WHERE "Id" = (
                        SELECT "Id"
                        FROM "configuracoes_fiscais"
                        WHERE "EmpresaId" = $empresaId
                          AND "Ativo" = 1
                        LIMIT 1
                    )
                    RETURNING "{serieColumn}" AS "Serie",
                              "{numeroColumn}" - 1 AS "Numero";
                    """;

                var empresaIdParameter = command.CreateParameter();
                empresaIdParameter.ParameterName = "$empresaId";
                empresaIdParameter.Value = empresaId;
                command.Parameters.Add(empresaIdParameter);

                var updatedAtParameter = command.CreateParameter();
                updatedAtParameter.ParameterName = "$updatedAt";
                updatedAtParameter.Value = DateTime.UtcNow;
                command.Parameters.Add(updatedAtParameter);

                int serie;
                long numero;

                await using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken))
                {
                    if (!await reader.ReadAsync(cancellationToken))
                        throw new InvalidOperationException("Configuração fiscal não encontrada.");

                    serie = reader.GetInt32(reader.GetOrdinal("Serie"));
                    numero = reader.GetInt64(reader.GetOrdinal("Numero"));
                }

                return (serie, numero);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 5 && tentativa < maxTentativas)
            {
                await Task.Delay(50 * tentativa, cancellationToken);
            }
        }

        throw new InvalidOperationException("Não foi possível reservar a numeração fiscal após múltiplas tentativas.");
    }
}