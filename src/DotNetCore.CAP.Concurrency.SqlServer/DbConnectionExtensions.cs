using System.Data;

namespace DotNetCore.CAP.Concurrency.SqlServer
{
    internal static class DbConnectionExtensions
    {
        public static int ExecuteNonQuery(
            this IDbConnection connection,
            string sql,
            IDbTransaction? transaction = null,
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

            if (transaction != null)
            {
                command.Transaction = transaction;
            }

            return command.ExecuteNonQuery();
        }

        public static object ExecuteScalar(
            this IDbConnection connection,
            string sql,
            IDbTransaction? transaction = null,
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

            if (transaction != null)
            {
                command.Transaction = transaction;
            }

            return command.ExecuteScalar();
        }
    }
}
