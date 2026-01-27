using Microsoft.EntityFrameworkCore;

namespace CodeQuest.Context
{
    public class UsersContext : DbContext
    {
        public DbSet<Model.Users> Users { get; set; }

        public UsersContext()
        {
            Database.EnsureCreated();
            Users.Load();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseMySql("server=127.0.0.1;uid=root;pwd=;database=CodeQuest",
                new MySqlServerVersion(new Version(8, 0, 11)));
    }
}
