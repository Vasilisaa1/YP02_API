using CodeQuest.Model;
using Microsoft.EntityFrameworkCore;

namespace CodeQuest.Context
{
    public class QuizContext : DbContext
    {
        public DbSet<Quiz> Quiz { get; set; }

        // Добавляем DbSet для Topics, если его нет
        public DbSet<Topics> Topics { get; set; }

        public QuizContext()
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
       
            modelBuilder.Entity<Quiz>()
                .HasOne<Topics>()
                .WithMany()
                .HasForeignKey(q => q.topic_id)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
