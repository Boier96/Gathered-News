namespace Gathered_News.ViewModels
{
    public class ArticleCardViewModel
    {
        public int Id { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}