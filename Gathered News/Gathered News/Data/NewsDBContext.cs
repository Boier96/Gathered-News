using Gathered_News.Models;
using Microsoft.EntityFrameworkCore;

namespace Gathered_News.Data
{
    public class NewsDbContext : DbContext
    {
        public NewsDbContext(DbContextOptions<NewsDbContext> options) : base(options)
        {
        }

        public DbSet<ArticleModel> Articles => Set<ArticleModel>();
        public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
        public DbSet<ArchivedArticle> ArchivedArticles => Set<ArchivedArticle>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ArticleModel>(entity =>
            {
                entity.ToTable("Articles");
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Title).IsRequired().HasMaxLength(500);
                entity.Property(a => a.Url).HasMaxLength(2000);
                entity.Property(a => a.ImageUrl).HasMaxLength(2000);
                entity.Property(a => a.Source).HasMaxLength(200);
                entity.HasIndex(a => a.Url).IsUnique();
            });

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("AppUsers");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.UserName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
                entity.HasIndex(u => u.UserName).IsUnique();
            });

            modelBuilder.Entity<ArchivedArticle>(entity =>
            {
                entity.ToTable("ArchivedArticles");
                entity.HasKey(a => a.Id);
                entity.Property(a => a.ArchivedAt).IsRequired();
                entity.HasIndex(a => new { a.UserId, a.ArticleId }).IsUnique();
                entity.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(a => a.Article).WithMany().HasForeignKey(a => a.ArticleId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
