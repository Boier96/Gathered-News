using Microsoft.EntityFrameworkCore;

namespace Gathered_News.Data
{
    public static class DatabaseInitializer
    {
        public static async Task EnsureAuthTablesAsync(NewsDbContext db, CancellationToken cancellationToken = default)
        {
            var statements = new[]
            {
                """
                CREATE TABLE IF NOT EXISTS "AppUsers" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_AppUsers" PRIMARY KEY AUTOINCREMENT,
                    "UserName" TEXT NOT NULL,
                    "PasswordHash" TEXT NOT NULL,
                    "CreatedAt" TEXT NOT NULL
                )
                """,
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_AppUsers_UserName"
                    ON "AppUsers" ("UserName")
                """,
                """
                CREATE TABLE IF NOT EXISTS "ArchivedArticles" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_ArchivedArticles" PRIMARY KEY AUTOINCREMENT,
                    "UserId" INTEGER NOT NULL,
                    "ArticleId" INTEGER NOT NULL,
                    "ArchivedAt" TEXT NOT NULL,
                    CONSTRAINT "FK_ArchivedArticles_AppUsers_UserId"
                        FOREIGN KEY ("UserId") REFERENCES "AppUsers" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_ArchivedArticles_Articles_ArticleId"
                        FOREIGN KEY ("ArticleId") REFERENCES "Articles" ("Id") ON DELETE CASCADE
                )
                """,
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_ArchivedArticles_UserId_ArticleId"
                    ON "ArchivedArticles" ("UserId", "ArticleId")
                """
            };

            foreach (var statement in statements)
            {
                await db.Database.ExecuteSqlRawAsync(statement, cancellationToken);
            }
        }
    }
}
