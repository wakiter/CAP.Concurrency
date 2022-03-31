using Microsoft.EntityFrameworkCore;

namespace DotNetCore.CAP.Concurrency.SqlServer.Tests.Integration
{
    public interface IKeepDatabaseSettings
    {
        string? ConnectionString { get; }
    }

    public interface IStoreDatabaseSettings
    {
        void StoreConnectionString(string? connectionString);
    }

    internal sealed class DatabaseSettingsStorage : IKeepDatabaseSettings, IStoreDatabaseSettings
    {
        public string? ConnectionString { get; private set; }

        public void StoreConnectionString(string? connectionString)
        {
            ConnectionString = connectionString;
        }
    }

    public class AppDbContext : DbContext
    {
        private readonly IKeepDatabaseSettings _databaseSettings;

        public AppDbContext(IKeepDatabaseSettings databaseSettings)
        {
            _databaseSettings = databaseSettings;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_databaseSettings.ConnectionString!);
        }
    }
}