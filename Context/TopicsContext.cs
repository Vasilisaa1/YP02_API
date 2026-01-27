using Microsoft.EntityFrameworkCore;

namespace CodeQuest.Context
{
    public class TopicsContext : DbContext
    {
        public DbSet<Model.Topics> Topics { get; set; }

        public TopicsContext()
        {
            Database.EnsureCreated();
            Topics.Load();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseMySql("server=127.0.0.1;uid=root;pwd=;database=CodeQuest",
                new MySqlServerVersion(new Version(8, 0, 11)));
    }
}
