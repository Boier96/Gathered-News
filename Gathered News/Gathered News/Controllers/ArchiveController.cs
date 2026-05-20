using System.Security.Claims;
using Gathered_News.Data;
using Gathered_News.Models;
using Gathered_News.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gathered_News.Controllers
{
    [Authorize]
    public class ArchiveController : Controller
    {
        private readonly NewsDbContext _db;

        public ArchiveController(NewsDbContext db)
        {
            _db = db;
        }

        // displays all archived articles, sorts by when added
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            if (userId is null)
                return Challenge();

            var archived = await _db.ArchivedArticles
                .AsNoTracking()
                .Where(a => a.UserId == userId.Value)
                .OrderByDescending(a => a.ArchivedAt)
                .Select(a => new ArchivedArticleViewModel
                {
                    ArchiveId = a.Id,
                    ArticleId = a.ArticleId,
                    Source = a.Article!.Source,
                    Title = a.Article.Title,
                    ImageUrl = a.Article.ImageUrl,
                    ArchivedAt = a.ArchivedAt
                })
                .ToListAsync();

            return View(new ArchiveIndexViewModel
            {
                UserName = User.Identity?.Name ?? "Reader",
                Articles = archived
            });
        }

        // toggles archive status of article
        // returns JSON to indicate new archived state
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int articleId)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized(new { success = false });

            var archive = await _db.ArchivedArticles.SingleOrDefaultAsync(a => a.UserId == userId.Value && a.ArticleId == articleId);
            if (archive is null)
            {
                var articleExists = await _db.Articles.AnyAsync(a => a.Id == articleId);
                if (!articleExists)
                    return NotFound(new { success = false });

                archive = new ArchivedArticle
                {
                    UserId = userId.Value,
                    ArticleId = articleId,
                    ArchivedAt = DateTime.UtcNow
                };

                _db.ArchivedArticles.Add(archive);
                await _db.SaveChangesAsync();
                return Json(new { success = true, archived = true });
            }

            _db.ArchivedArticles.Remove(archive);
            await _db.SaveChangesAsync();
            return Json(new { success = true, archived = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Remove(int articleId) => Toggle(articleId);

        private int? GetUserId()
        {
            var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idValue, out var id) ? id : null;
        }
    }
}
