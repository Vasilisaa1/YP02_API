using Microsoft.EntityFrameworkCore;

namespace CodeQuest.Context
{
    public class AdminContext : DbContext
    {
        public DbSet<Model.Admin> Admins { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("server=127.0.0.1;uid=root;pwd=;database=CodeQuest",
                new MySqlServerVersion(new Version(8, 0, 11)));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Model.Admin>(entity =>
            {
                entity.ToTable("admins"); // или какое имя таблицы у вас в БД
                entity.HasKey(e => e.id);
                entity.Property(e => e.id).ValueGeneratedOnAdd();
                entity.Property(e => e.login).HasMaxLength(20);
                entity.Property(e => e.password).HasMaxLength(20);
            });
        }
    }
}
