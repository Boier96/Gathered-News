namespace Gathered_News.Data
{
    using Microsoft.EntityFrameworkCore;
    using models = Gathered_News.Models;

    public class NewsDbContext : DbContext
    {
        public NewsDbContext(DbContextOptions<NewsDbContext> options) : base(options)
        {
        }

        public DbSet<models.ArticleModel> Articles => Set<models.ArticleModel>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<models.ArticleModel>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Title)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(a => a.Url)
                    .HasMaxLength(2000);

                entity.Property(a => a.Source)
                    .HasMaxLength(200);

                entity.HasIndex(a => a.Url)
                    .IsUnique();
            });
        }
    }
}