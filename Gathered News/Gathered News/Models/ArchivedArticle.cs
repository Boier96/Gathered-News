namespace Gathered_News.Models
{
    public class ArchivedArticle
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public int ArticleId { get; set; }
        public ArticleModel? Article { get; set; }
        public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;
    }
}
