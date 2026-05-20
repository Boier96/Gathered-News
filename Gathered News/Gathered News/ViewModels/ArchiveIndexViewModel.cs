namespace Gathered_News.ViewModels
{
    public class ArchiveIndexViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public List<ArchivedArticleViewModel> Articles { get; set; } = new();
    }
}
