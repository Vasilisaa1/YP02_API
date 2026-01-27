using Microsoft.EntityFrameworkCore;

namespace CodeQuest.Context
{
    public class UserProgressContext : DbContext
    {
        public DbSet<Model.UserProgress> UserProgress { get; set; }

        public UserProgressContext()
        {
            Database.EnsureCreated();
            UserProgress.Load();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseMySql("server=127.0.0.1;uid=root;pwd=;database=CodeQuest",
                new MySqlServerVersion(new Version(8, 0, 11)));
    }
}
