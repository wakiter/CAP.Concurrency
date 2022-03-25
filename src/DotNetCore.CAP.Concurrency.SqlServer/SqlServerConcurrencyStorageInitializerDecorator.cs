using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Concurrency.SqlServer
{
    internal sealed class SqlServerConcurrencyStorageInitializerDecorator : IStorageInitializer
    {
        private readonly IStorageInitializer _inner;
        private readonly ILogger _logger;
        private readonly SqlServerConcurrencyOptions _options;
        private readonly IOptions<SqlServerOptions> _sqlServerOptions;

        public SqlServerConcurrencyStorageInitializerDecorator(
            IStorageInitializer inner,
            ILogger<SqlServerConcurrencyStorageInitializerDecorator> logger, 
            SqlServerConcurrencyOptions options, 
            IOptions<SqlServerOptions> sqlServerOptions)
        {
            _inner = inner;
            _logger = logger;
            _options = options;
            _sqlServerOptions = sqlServerOptions;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _logger.LogDebug("Ensuring all messages in flight database tables script are applied...");

            var sql = CreateDbTablesScript(_options.Schema, _options.MessagesReceivedInFlightStorageName, _inner.GetReceivedTableName());
            await using var connection = new SqlConnection(_sqlServerOptions.Value.ConnectionString);
            connection.ExecuteNonQuery(sql);

            _logger.LogDebug("Ensured all messages in flight database tables script are applied.");

            await _inner.InitializeAsync(cancellationToken);
        }

        public string GetPublishedTableName()
        {
            return _inner.GetPublishedTableName();
        }

        public string GetReceivedTableName()
        {
            return _inner.GetReceivedTableName();
        }

        private string CreateDbTablesScript(string schema, string messagesReceivedInFlightTableName, string receivedMessagesTableNameWithSchema)
        {
            var sql = $@"

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{schema}')
BEGIN
	EXEC('CREATE SCHEMA [{schema}]')
END;

IF OBJECT_ID(N'{schema}.{messagesReceivedInFlightTableName}',N'U') IS NULL
BEGIN
    CREATE TABLE {schema}.{messagesReceivedInFlightTableName} 
    (
        [Id] [bigint] IDENTITY(1, 1) NOT NULL,
        [MessageId] [bigint] NOT NULL FOREIGN KEY REFERENCES {receivedMessagesTableNameWithSchema}(Id) ON DELETE CASCADE,
        [InFlightTo] [datetime2](7) NOT NULL,
        CONSTRAINT [PK_{messagesReceivedInFlightTableName}] PRIMARY KEY CLUSTERED
        (
            [Id] ASC
        ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY] 
END

IF NOT EXISTS(SELECT * FROM sys.indexes WHERE name = 'FK_{messagesReceivedInFlightTableName}_IDX' AND object_id = OBJECT_ID('{schema}.{messagesReceivedInFlightTableName}'))
BEGIN
    CREATE UNIQUE INDEX FK_{messagesReceivedInFlightTableName}_IDX ON {schema}.{messagesReceivedInFlightTableName}(MessageId)
END
";

            return sql;
        }
    }
}