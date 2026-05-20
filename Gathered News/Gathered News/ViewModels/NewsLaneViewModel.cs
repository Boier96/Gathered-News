namespace Gathered_News.ViewModels
{
    public class NewsLaneViewModel
    {
        public List<ArticleCardViewModel> LeftLane { get; set; } = new();
        public List<ArticleCardViewModel> RightLane { get; set; } = new();
        public bool IsAuthenticated { get; set; }
        public string? UserName { get; set; }
        public int ArchivedCount { get; set; }
    }
}
