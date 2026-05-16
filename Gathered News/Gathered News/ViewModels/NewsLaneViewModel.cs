namespace Gathered_News.ViewModels
{
    public class NewsLaneViewModel
    {
        public List<ArticleCardViewModel> LeftLane { get; set; } = new();
        public List<ArticleCardViewModel> RightLane { get; set; } = new();
    }
}