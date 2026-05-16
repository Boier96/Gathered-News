import re
from base import BaseNewsScraper


class DWScraper(BaseNewsScraper):

    name = "dw"
    index_url = "https://www.dw.com/en/top-stories/s-9097"
    base_domain = "www.dw.com"
    output_file = "dw_articles.json"

    ARTICLE_PATH_PATTERN = re.compile(r"^/en/.+/a-\d+$")

    def collect_article_urls(self):
        soup = self.get_soup(self.index_url)
        if soup is None:
            return []

        seen = set()
        urls = []

        for tag in soup.find_all("a", href=True):
            href = tag["href"]

            if href.startswith("http") and "dw.com" not in href:
                continue

            path = href if href.startswith("/") else "/" + href.split("dw.com", 1)[-1]

            if not self.ARTICLE_PATH_PATTERN.match(path):
                continue

            full_url = self.normalise_url(href)
            if full_url not in seen:
                seen.add(full_url)
                urls.append(full_url)

        self.log.info("Found %d article URLs.", len(urls))
        return urls

    def scrape_article(self, url):
        soup = self.get_soup(url)
        if soup is None:
            return None

        h1 = soup.find("h1")
        title = h1.get_text(strip=True) if h1 else ""
        if not title:
            title_tag = soup.find("title")
            title = title_tag.get_text(strip=True) if title_tag else "Unknown title"

        paragraphs = []

        body = soup.find("div", class_=re.compile(r"\brich-text\b"))
        if not body:
            body = soup.find("div", attrs={"data-tracking-name": "article-body"})
        if not body:
            body = soup.find("article") or soup

        for p in body.find_all("p"):
            text = p.get_text(strip=True)
            if text:
                paragraphs.append(text)

        return {
            "source": self.name,
            "url": url,
            "title": title,
            "content": paragraphs,
            "image_url": self.get_primary_image(soup),
        }