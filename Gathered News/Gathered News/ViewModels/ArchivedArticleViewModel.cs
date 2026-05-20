namespace Gathered_News.ViewModels
{
    public class ArchivedArticleViewModel
    {
        public int ArchiveId { get; set; }
        public int ArticleId { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime ArchivedAt { get; set; }
    }
}
