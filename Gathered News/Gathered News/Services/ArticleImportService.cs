using System.Text.Json;
using Gathered_News.Data;
using models = Gathered_News.Models;
using Microsoft.EntityFrameworkCore;

namespace Gathered_News.Services
{
    public class ArticleImportService
    {
        private readonly NewsDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<ArticleImportService> _logger;

        public ArticleImportService(
            NewsDbContext db,
            IWebHostEnvironment env,
            IConfiguration config,
            ILogger<ArticleImportService> logger)
        {
            _db = db;
            _env = env;
            _config = config;
            _logger = logger;
        }

        // entry point, reads JSON file from folder (set in appsettings as ScraperJsonFolder), parses articles
        // deduplicates against database, inserts new articles in transaction
        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            var relativeFolder = _config["ScraperJsonFolder"] ?? "webscraperTesting";
            var folder = Path.GetFullPath(Path.Combine(_env.ContentRootPath, relativeFolder));

            _logger.LogInformation("Looking for JSON files in: {Folder}", folder);

            if (!Directory.Exists(folder))
            {
                _logger.LogWarning("JSON folder does not exist: {Folder}", folder);
                return;
            }

            await RemoveNonArticleRecordsAsync(cancellationToken);

            var files = Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories).ToList();
            _logger.LogInformation("found {Count} JSON files.", files.Count);

            if (files.Count == 0)
                return;

            var stagedArticles = new List<StagedArticle>();

            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file, cancellationToken);

                    var options = new JsonDocumentOptions
                    {
                        AllowTrailingCommas = true,
                        CommentHandling = JsonCommentHandling.Skip
                    };

                    using var doc = JsonDocument.Parse(json, options);

                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        TryAddArticle(stagedArticles, doc.RootElement, file);
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var element in doc.RootElement.EnumerateArray())
                        {
                            if (element.ValueKind == JsonValueKind.Object)
                                TryAddArticle(stagedArticles, element, file);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Skipping unsupported JSON root in {File}", file);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Invalid JSON in {File}", file);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import {File}", file);
                }
            }

            if (stagedArticles.Count == 0)
            {
                _logger.LogInformation("No importable articles found.");
                return;
            }

            var dedupedIncoming = stagedArticles
                .GroupBy(x => x.DedupKey, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            var existingKeys = await _db.Articles
                .AsNoTracking()
                .Select(a => new
                {
                    a.Url,
                    a.Source,
                    a.Title
                })
                .ToListAsync(cancellationToken);

            var existingSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in existingKeys)
            {
                var key = BuildDedupKey(item.Url, item.Source, item.Title);
                if (!string.IsNullOrWhiteSpace(key))
                    existingSet.Add(key);
            }

            var toInsert = dedupedIncoming
                .Where(x => !existingSet.Contains(x.DedupKey))
                .Select(x => x.Article)
                .ToList();

            _logger.LogInformation(
                "prepared {IncomingCount} articles, deduped to {DedupedCount}, inserting {InsertCount}.",
                stagedArticles.Count,
                dedupedIncoming.Count,
                toInsert.Count);

            if (toInsert.Count == 0)
            {
                _logger.LogInformation("nothing new to insert");
                return;
            }

            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

            await _db.Articles.AddRangeAsync(toInsert, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);

            _logger.LogInformation("completed successfully.");
        }

        // attempts parse single JSON element, casting to article
        // compares elements to "importable candiate" check
        private void TryAddArticle(List<StagedArticle> articles, JsonElement element, string filePath)
        {
            var source = GetString(element, "source") ?? Path.GetFileNameWithoutExtension(filePath);
            var url = GetString(element, "url");
            var title = GetString(element, "title");
            var imageUrl = GetString(element, "image_url");

            if (!IsImportableCandidate(source, url, title))
            {
                _logger.LogInformation("skipping alleged non-article entry from {File}: {Title}", filePath, title ?? "(no title)");
                return;
            }

            var dedupKey = BuildDedupKey(url, source, title);

            if (string.IsNullOrWhiteSpace(dedupKey))
            {
                _logger.LogWarning("skipping article in {File} because it has no usable url/source/title.", filePath);
                return;
            }

            var contentText = BuildContentText(element);
            var contentJson = BuildContentJson(element);

            var article = new models.ArticleModel
            {
                Source = source,
                Url = url ?? string.Empty,
                Title = title ?? string.Empty,
                Content = contentText,
                ContentJson = contentJson,
                ImageUrl = imageUrl,
                RawJson = element.GetRawText(),
                ImportedAt = DateTime.UtcNow
            };

            articles.Add(new StagedArticle(dedupKey, article));
        }

        // builds representation of content field
        // if arrau of strs, join w double newL
        // if single str, use directly
        private static string? BuildContentText(JsonElement element)
        {
            if (!element.TryGetProperty("content", out var contentProp))
                return null;

            if (contentProp.ValueKind == JsonValueKind.Array)
            {
                var paragraphs = contentProp.EnumerateArray()
                    .Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                return paragraphs.Count == 0
                    ? null
                    : string.Join("\n\n", paragraphs!);
            }

            if (contentProp.ValueKind == JsonValueKind.String)
                return contentProp.GetString();

            return null;
        }

        private static string? BuildContentJson(JsonElement element)
        {
            if (!element.TryGetProperty("content", out var contentProp))
                return null;

            return contentProp.ValueKind switch
            {
                JsonValueKind.Array => contentProp.GetRawText(),
                JsonValueKind.String => JsonSerializer.Serialize(contentProp.GetString()),
                _ => null
            };
        }

        private static string? GetString(JsonElement element, string name)
        {
            if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                var value = prop.GetString();
                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }

            return null;
        }


        private async Task RemoveNonArticleRecordsAsync(CancellationToken cancellationToken)
        {
            var existingArticles = await _db.Articles
                .ToListAsync(cancellationToken);

            var invalidArticles = existingArticles
                .Where(article => !IsImportableCandidate(article.Source, article.Url, article.Title))
                .ToList();

            if (invalidArticles.Count == 0)
                return;

            _logger.LogInformation("removing {Count} non-article records already in the database.", invalidArticles.Count);

            _db.Articles.RemoveRange(invalidArticles);
            await _db.SaveChangesAsync(cancellationToken);
        }

        // true if data could be real article
        // filters are incomplete, if non-articles make it into db please add that function
        private static bool IsImportableCandidate(string? source, string? url, string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return false;

            var normalizedTitle = NormalizeTitle(title);

            if (normalizedTitle.Length == 0)
                return false;

            if (normalizedTitle.Equals("unknown title", StringComparison.OrdinalIgnoreCase))
                return false;

            if (BlockedTitles.Contains(normalizedTitle))
                return false;

            if (source?.Equals("cbc", StringComparison.OrdinalIgnoreCase) == true &&
                normalizedTitle.StartsWith("About ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(url) && LooksLikeUtilityUrl(url))
                return false;

            return true;
        }

        private static string NormalizeTitle(string title)
        {
            return string.Join(" ", title.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)).Trim();
        }

        private static bool LooksLikeUtilityUrl(string url)
        {
            var lower = url.Trim().ToLowerInvariant();

            return UtilityUrlFragments.Any(fragment => lower.Contains(fragment));
        }


        // add more if you see them
        private static readonly HashSet<string> BlockedTitles = new(StringComparer.OrdinalIgnoreCase)
        {
            "About CBC News",
            "Unknown title",
            "Data Privacy Policy",
            "Legal notice",
            "Accessibility statement"
        };

        private static readonly string[] UtilityUrlFragments =
        {
            "/about-cbc-news",
            "/legal-notice",
            "/accessibility-statement",
            "/privacy-policy",
            "/terms-of-use",
            "/cookie-policy",
            "/contact-us",
            "/sitemap"
        };

        // deduplication key, prefers URL, falls back to source/title
        // rtrns null if fields empty
        private static string? BuildDedupKey(string? url, string? source, string? title)
        {
            if (!string.IsNullOrWhiteSpace(url))
                return $"url:{url.Trim()}";

            if (!string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(title))
                return $"fallback:{source.Trim()}|{title.Trim()}";

            return null;
        }
        private sealed record StagedArticle(string DedupKey, models.ArticleModel Article);
    }
}