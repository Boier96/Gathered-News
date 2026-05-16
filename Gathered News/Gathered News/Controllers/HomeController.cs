using Gathered_News.Data;
using Gathered_News.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gathered_News.Controllers
{
    public class HomeController : Controller
    {
        private readonly NewsDbContext _db;

        public HomeController(NewsDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var cutoff = DateTime.UtcNow.AddHours(-24);

            var recent = await _db.Articles
                .AsNoTracking()
                .Where(a => a.ImportedAt >= cutoff)
                .OrderByDescending(a => a.ImportedAt)
                .Select(a => new ArticleCardViewModel
                {
                    Id = a.Id,
                    Source = a.Source,
                    Title = a.Title,
                    Url = a.Url,
                    ImageUrl = a.ImageUrl
                })
                .ToListAsync();

            if (recent.Count == 0)
            {
                recent = await _db.Articles
                    .AsNoTracking()
                    .OrderByDescending(a => a.ImportedAt)
                    .Take(40)
                    .Select(a => new ArticleCardViewModel
                    {
                        Id = a.Id,
                        Source = a.Source,
                        Title = a.Title,
                        Url = a.Url,
                        ImageUrl = a.ImageUrl
                    })
                    .ToListAsync();
            }

            var spread = SpreadByOutlet(recent);
            var midpoint = (spread.Count + 1) / 2;

            var vm = new NewsLaneViewModel
            {
                LeftLane = spread.Take(midpoint).ToList(),
                RightLane = spread.Skip(midpoint).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetArticle(int id)
        {
            var article = await _db.Articles
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => new
                {
                    a.Id,
                    a.Source,
                    a.Title,
                    a.Url,
                    a.ImageUrl,
                    a.Content
                })
                .FirstOrDefaultAsync();

            if (article == null)
                return NotFound();

            return Json(article);
        }

        private static List<ArticleCardViewModel> SpreadByOutlet(List<ArticleCardViewModel> articles)
        {
            var groups = articles
                .GroupBy(a => a.Source, StringComparer.OrdinalIgnoreCase)
                .Select(g => new Queue<ArticleCardViewModel>(g))
                .ToList();

            var result = new List<ArticleCardViewModel>();

            while (groups.Any(q => q.Count > 0))
            {
                foreach (var queue in groups)
                {
                    if (queue.Count > 0)
                        result.Add(queue.Dequeue());
                }
            }

            return result;
        }
    }
}