using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Monitoring;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Concurrency.SqlServer.Tests.Integration;

internal sealed class SqlServerDataStorageWithFasterRetriesDecorator : IDataStorage
{
    public int OverallNumberOfMessagesNeedingRetryReturned { get; private set; }
    public int OverallNumberOfMessagesNeedingRetryInvocation { get; private set; }

    private readonly IDataStorage _inner;
    private readonly IOptions<CapOptions> _capOptions;
    private readonly IOptions<SqlServerOptions> _options;
    private readonly ISerializer _serializer;

    private readonly string _receivingTable;
    private string _publishingTable;

    public SqlServerDataStorageWithFasterRetriesDecorator(
        IDataStorage inner,
        IOptions<CapOptions> capOptions,
        IOptions<SqlServerOptions> options, 
        ISerializer serializer,
        IStorageInitializer initializer)
    {
        _inner = inner;
        _capOptions = capOptions;
        _options = options;
        _serializer = serializer;

        _receivingTable = initializer.GetReceivedTableName();
        _publishingTable = initializer.GetPublishedTableName();
    }

    public Task ChangePublishStateAsync(MediumMessage message, StatusName state)
    {
        return _inner.ChangePublishStateAsync(message, state);
    }

    public Task ChangeReceiveStateAsync(MediumMessage message, StatusName state)
    {
        return _inner.ChangeReceiveStateAsync(message, state);
    }

    public MediumMessage StoreMessage(string name, Message content, object? dbTransaction = null)
    {
        return _inner.StoreMessage(name, content, dbTransaction);
    }

    public void StoreReceivedExceptionMessage(string name, string @group, string content)
    {
        _inner.StoreReceivedExceptionMessage(name, @group, content);
    }

    public MediumMessage StoreReceivedMessage(string name, string @group, Message content)
    {
        return _inner.StoreReceivedMessage(name, @group, content);
    }

    public Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000,
        CancellationToken token = new CancellationToken())
    {
        return _inner.DeleteExpiresAsync(table, timeout, batchCount, token);
    }

    public Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry()
    {
        return _inner.GetPublishedMessagesOfNeedRetry();
    }

    public Task<IEnumerable<MediumMessage>> GetReceivedMessagesOfNeedRetry()
    {
        return GetMessagesOfNeedRetryAsync(_receivingTable);
    }

    public IMonitoringApi GetMonitoringApi()
    {
        return _inner.GetMonitoringApi();
    }

    public async Task TruncateReceivedAndPublishedTable()
    {
        var sql = $"DELETE FROM {_receivingTable}; DELETE FROM {_publishingTable};";
        await using var connection = new SqlConnection(_options.Value.ConnectionString);
        ExecuteNonQuery(connection, sql);
    }

    private async Task<IEnumerable<MediumMessage>> GetMessagesOfNeedRetryAsync(string tableName)
    {
        var fourMinAgo = DateTime.Now.AddSeconds(-_capOptions.Value.FailedRetryInterval).ToString("O");
        var sql =
            $"SELECT TOP (200) Id, Content, Retries, Added FROM {tableName} WITH (readpast) WHERE Retries<{_capOptions.Value.FailedRetryCount} " +
            $"AND Version='{_capOptions.Value.Version}' AND Added<'{fourMinAgo}' AND (StatusName = '{StatusName.Failed}' OR StatusName = '{StatusName.Scheduled}')";

        List<MediumMessage> result;
        await using (var connection = new SqlConnection(_options.Value.ConnectionString))
        {
            result = ExecuteReader(connection, sql, reader =>
            {
                var messages = new List<MediumMessage>();
                while (reader.Read())
                {
                    messages.Add(new MediumMessage
                    {
                        DbId = reader.GetInt64(0).ToString(),
                        Origin = _serializer.Deserialize(reader.GetString(1))!,
                        Retries = reader.GetInt32(2),
                        Added = reader.GetDateTime(3)
                    });
                }

                return messages;
            });
        }

        OverallNumberOfMessagesNeedingRetryReturned += result.Count;
        OverallNumberOfMessagesNeedingRetryInvocation += 1;

        return result;
    }

    private static T ExecuteReader<T>(
        IDbConnection connection, 
        string sql, 
        Func<IDataReader, T>? readerFunc,
        params object[] sqlParams)
    {
        if (connection.State == ConnectionState.Closed)
        {
            connection.Open();
        }

        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = sql;
        foreach (var param in sqlParams)
        {
            command.Parameters.Add(param);
        }

        var reader = command.ExecuteReader();

        T result = default!;
        if (readerFunc != null)
        {
            result = readerFunc(reader);
        }

        return result;
    }

    private static void ExecuteNonQuery(IDbConnection connection, string sql)
    {
        if (connection.State == ConnectionState.Closed)
        {
            connection.Open();
        }

        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = sql;

        command.ExecuteNonQuery();
    }
}