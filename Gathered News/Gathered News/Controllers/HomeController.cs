using data = Gathered_News.Data;
using models = Gathered_News.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gathered_News.Controllers
{
    public class HomeController : Controller
    {
        private readonly data.NewsDbContext _db;

        public HomeController(data.NewsDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var articles = await _db.Articles
                .Select(a => new models.ArticleModel
                {
                    Id = a.Id,
                    Title = a.Title,
                    Content = a.Content,
                    Url = a.Url,
                    Source = a.Source,
                    ImportedAt = a.ImportedAt
                })
                .ToListAsync();

            return View(articles);
        }
    }
}