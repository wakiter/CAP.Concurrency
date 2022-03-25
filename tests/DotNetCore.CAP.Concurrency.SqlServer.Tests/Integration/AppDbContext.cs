using Microsoft.EntityFrameworkCore;

namespace DotNetCore.CAP.Concurrency.SqlServer.Tests.Integration
{
    public class AppDbContext : DbContext
    {
        public const string ConnectionString =
            "data source=localhost;initial catalog=CAP;persist security info=True;Integrated Security=SSPI;";

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(ConnectionString);
        }
    }
}