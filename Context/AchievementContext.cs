using CodeQuest.Model;
using Microsoft.EntityFrameworkCore;

namespace CodeQuest.Context
{
    public class AchievementContext : DbContext
    {
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<UserAchievement> UserAchievements { get; set; }

        public AchievementContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("server=127.0.0.1;uid=root;pwd=;database=CodeQuest",
                new MySqlServerVersion(new Version(8, 0, 11)));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserAchievement>()
                .HasIndex(ua => new { ua.user_id, ua.achievement_id })
                .IsUnique();
        }
    }
}