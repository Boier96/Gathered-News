using System.Security.Claims;
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

        // the logic for which articles to place in the "lanes"
        // looks for articles within the last 24 hours first, but if it fails, just picks the last 40 articles
        // also ensures that if possible, articles should be split up by outlets
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

            return View(new NewsLaneViewModel
            {
                LeftLane = spread.Take(midpoint).ToList(),
                RightLane = spread.Skip(midpoint).ToList(),
                IsAuthenticated = User.Identity?.IsAuthenticated == true,
                UserName = User.Identity?.Name,
                ArchivedCount = await GetArchiveCountAsync()
            });
        }

        // full article view, returns if user has archived this article

        [HttpGet]
        public async Task<IActionResult> Article(int id, string? returnUrl = null)
        {
            var userId = GetUserId();
            var article = await _db.Articles
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => new ArticleDetailViewModel
                {
                    Id = a.Id,
                    Source = a.Source,
                    Title = a.Title,
                    Url = a.Url,
                    ImageUrl = a.ImageUrl,
                    Content = a.Content
                })
                .FirstOrDefaultAsync();

            if (article == null)
                return NotFound();

            if (userId.HasValue)
            {
                article.IsArchived = await _db.ArchivedArticles.AsNoTracking()
                    .AnyAsync(a => a.ArticleId == id && a.UserId == userId.Value);
            }

            article.IsAuthenticated = User.Identity?.IsAuthenticated == true;
            article.UserName = User.Identity?.Name;
            article.ReturnUrl = returnUrl;

            return View(article);
        }

        // JSON endpoint for loading of article
        [HttpGet]
        public async Task<IActionResult> GetArticle(int id)
        {
            var userId = GetUserId();
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

            var isArchived = false;
            if (userId.HasValue)
            {
                isArchived = await _db.ArchivedArticles.AsNoTracking().AnyAsync(a => a.ArticleId == id && a.UserId == userId.Value);
            }

            return Json(new
            {
                article.Id,
                article.Source,
                article.Title,
                article.Url,
                article.ImageUrl,
                article.Content,
                IsArchived = isArchived
            });
        }

        private async Task<int> GetArchiveCountAsync()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return 0;

            return await _db.ArchivedArticles.AsNoTracking().CountAsync(a => a.UserId == userId.Value);
        }

        private int? GetUserId()
        {
            var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idValue, out var id) ? id : null;
        }

        // groups by source, then picks one from each group, spreading out articles across sources
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
