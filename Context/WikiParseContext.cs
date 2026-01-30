using Microsoft.EntityFrameworkCore;

namespace CodeQuest.Context
{
    public class WikiParseContext : DbContext
    {
        public DbSet<Model.WikiParseResult> WikiParseResults { get; set; }

        public WikiParseContext()
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
            modelBuilder.Entity<Model.WikiParseResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Title).HasMaxLength(500);
                entity.Property(e => e.Url).HasMaxLength(500);
                entity.Property(e => e.ContentSummary).HasColumnType("LONGTEXT");
                entity.Property(e => e.KeyFeatures).HasColumnType("LONGTEXT");
                entity.Property(e => e.VersionHistory).HasColumnType("LONGTEXT");
                entity.Property(e => e.RawHtml).HasColumnType("LONGTEXT");
                entity.Property(e => e.SiteType).HasMaxLength(50);
                entity.HasIndex(e => e.ParsedAt);
            });
        }
    }
}