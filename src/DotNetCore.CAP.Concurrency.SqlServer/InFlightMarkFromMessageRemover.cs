using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Concurrency.SqlServer
{
    internal sealed class InFlightMarkFromMessageRemover : IRemoveInFlightMarkFromMessage
    {
        private readonly IOptions<SqlServerOptions> _sqlServerOptions;
        private readonly SqlServerConcurrencyOptions _concurrencyOptions;

        public InFlightMarkFromMessageRemover(
            IOptions<SqlServerOptions> sqlServerOptions, 
            SqlServerConcurrencyOptions concurrencyOptions)
        {
            _sqlServerOptions = sqlServerOptions;
            _concurrencyOptions = concurrencyOptions;
        }

        public async Task RemoveInFlightMark(MediumMessage message, CancellationToken cancellationToken = new CancellationToken())
        {
            var sql = CreateRemoveInFlightMarkSql(_concurrencyOptions.Schema, _concurrencyOptions.MessagesReceivedInFlightStorageName, message);
            await using var connection = new SqlConnection(_sqlServerOptions.Value.ConnectionString);
            connection.ExecuteNonQuery(sql);
        }

        private string CreateRemoveInFlightMarkSql(string schema, string inFlightTableName, MediumMessage message)
        {
            return $"DELETE FROM {schema}.{inFlightTableName} WHERE MessageId = {message.DbId}";
        }
    }
}