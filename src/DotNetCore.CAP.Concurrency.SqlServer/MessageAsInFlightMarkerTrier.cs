using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Concurrency.SqlServer
{
    internal sealed class MessageAsInFlightMarkerTrier : ITryMarkingMessageAsInFlight
    {
        private readonly IOptions<SqlServerOptions> _sqlServerOptions;
        private readonly SqlServerConcurrencyOptions _concurrencyOptions;

        public MessageAsInFlightMarkerTrier(
            IOptions<SqlServerOptions> sqlServerOptions, 
            SqlServerConcurrencyOptions concurrencyOptions)
        {
            _sqlServerOptions = sqlServerOptions;
            _concurrencyOptions = concurrencyOptions;
        }

        public async Task<bool> TryMarkingAsInFlight(MediumMessage message, CancellationToken cancellationToken = new CancellationToken())
        {
            var sql = CreateInsertIntoFlightTableSql(_concurrencyOptions.Schema, _concurrencyOptions.MessagesReceivedInFlightStorageName, message);
            await using var connection = new SqlConnection(_sqlServerOptions.Value.ConnectionString);

            try
            {
                connection.ExecuteNonQuery(sql);
                return true;
            }
            catch (SqlException ex) when (
                ex.Number == 2627 // Unique constraint error
                || ex.Number == 2601 // Duplicated key row error || Constraint violation exception
                || ex.Number == 547 // Constraint check violation
                )
            {
                return false;
            }
        }

        private string CreateInsertIntoFlightTableSql(string schema, string inFlightTableName, MediumMessage message)
        {
            return
                $"DECLARE @inFlightTo DATETIME = (SELECT InFlightTo FROM {schema}.{inFlightTableName} WHERE MessageId = {message.DbId}); " +
                $"" +
                $"if (@inFlightTo IS NOT NULL) " +
                $"BEGIN " +
                $"   IF (@inFlightTo <= GETUTCDATE()) " +
                $"   BEGIN " +
                $"       DELETE FROM {schema}.{inFlightTableName} WHERE MessageId = {message.DbId}; " +
                $"   END " +
                $"END " +
                $"INSERT INTO {schema}.{inFlightTableName}(MessageId, InFlightTo) VALUES ({message.DbId}, '{DateTime.UtcNow.Add(_concurrencyOptions.InFlightTime)}'); ";
        }
    }
}