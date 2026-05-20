namespace Gathered_News.ViewModels
{
    public class ArticleDetailViewModel
    {
        public int Id { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? ImageUrl { get; set; }
        public string? Content { get; set; }
        public bool IsAuthenticated { get; set; }
        public string? UserName { get; set; }
        public bool IsArchived { get; set; }
        public string? ReturnUrl { get; set; }
    }
}
