namespace Gathered_News.Models
{
    public class ArticleModel
    {
        public int Id { get; set; }

        public string Source { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        public string? Content { get; set; }
        public string? ContentJson { get; set; }
        public string? ImageUrl { get; set; }

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
        public string RawJson { get; set; } = string.Empty;
    }
}