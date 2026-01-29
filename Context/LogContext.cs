using CodeQuest.Model;
using Microsoft.EntityFrameworkCore;

namespace CodeQuest.Context
{
    public class LogContext : DbContext
    {
        public DbSet<Log> Log { get; set; }

        public LogContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("server=127.0.0.1;uid=root;pwd=;database=CodeQuest",
                new MySqlServerVersion(new Version(8, 0, 11)));
        }
    }
}
