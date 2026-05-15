import re
from base import BaseNewsScraper


class BBCScraper(BaseNewsScraper):

    name = "bbc"
    index_url = "https://www.bbc.com/news"
    base_domain = "www.bbc.com"
    output_file = "bbc_articles.json"

    def collect_article_urls(self):
        soup = self.get_soup(self.index_url)
        if soup is None:
            return []

        seen = set()
        urls = []

        for tag in soup.find_all("a", href=True):
            href = tag["href"]
            if not re.search(r"/news/articles/", href):
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
        text_blocks = soup.find_all("div", attrs={"data-component": "text-block"})

        if text_blocks:
            for block in text_blocks:
                for p in block.find_all("p"):
                    text = p.get_text(strip=True)
                    if text:
                        paragraphs.append(text)
        else:
            container = soup.find("article") or soup
            for p in container.find_all("p"):
                text = p.get_text(strip=True)
                if text:
                    paragraphs.append(text)

        return {
            "source": self.name,
            "url": url,
            "title": title,
            "content": paragraphs,
        }
