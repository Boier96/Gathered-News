import re
import json
import time
import logging
import requests
import xml.etree.ElementTree as ET
from bs4 import BeautifulSoup, XMLParsedAsHTMLWarning
import warnings

warnings.filterwarnings("ignore", category=XMLParsedAsHTMLWarning)

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s  %(levelname)s  %(message)s",
    datefmt="%H:%M:%S",
)


class BaseNewsScraper:

    name = "base"
    index_url = ""
    output_file = "articles.json"
    request_delay = 1.0

    headers = {
        "User-Agent": (
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
            "AppleWebKit/537.36 (KHTML, like Gecko) "
            "Chrome/136.0.0.0 Safari/537.36"
        ),
        "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8",
        "Accept-Language": "en-US,en;q=0.9",
        "Accept-Encoding": "gzip, deflate, br",
        "Connection": "keep-alive",
        "Upgrade-Insecure-Requests": "1",
        "Sec-Fetch-Dest": "document",
        "Sec-Fetch-Mode": "navigate",
        "Sec-Fetch-Site": "none",
        "Sec-Fetch-User": "?1",
        "Cache-Control": "max-age=0",
    }

    def __init__(self):
        self.log = logging.getLogger(self.name)
        self.session = requests.Session()
        self.session.headers.update(self.headers)

    def get_soup(self, url):
        try:
            response = self.session.get(url, timeout=15)
            response.raise_for_status()
            return BeautifulSoup(response.text, "html.parser")
        except requests.RequestException as e:
            self.log.warning("Failed to fetch %s — %s", url, e)
            return None

    def get_rss_urls(self, rss_url):
        try:
            response = self.session.get(rss_url, timeout=15)
            response.raise_for_status()
            root = ET.fromstring(response.content)
            urls = []
            seen = set()
            for item in root.iter("item"):
                link = item.find("link")
                if link is not None and link.text:
                    url = link.text.strip().split("?")[0].split("#")[0]
                    if url not in seen:
                        seen.add(url)
                        urls.append(url)
            self.log.info("Found %d article URLs via RSS.", len(urls))
            return urls
        except (requests.RequestException, ET.ParseError) as e:
            self.log.warning("Failed to fetch RSS %s — %s", rss_url, e)
            return []

    def collect_article_urls(self):
        raise NotImplementedError

    def scrape_article(self, url):
        raise NotImplementedError

    def get_primary_image(self, soup):
        og = soup.find("meta", property="og:image")
        if og and og.get("content"):
            return og["content"].strip()

        for img in soup.find_all("img", src=True):
            src = img["src"].strip()
            if not src or src.startswith("data:"):
                continue
            if src.startswith("http"):
                return src
            return self.normalise_url(src)

        return None

    def normalise_url(self, href):
        if href.startswith("http"):
            return href.split("?")[0].split("#")[0]
        return ("https://" + self.base_domain + href).split("?")[0].split("#")[0]

    def run(self):
        self.log.info("Starting scraper for: %s", self.index_url)
        urls = self.collect_article_urls()

        if not urls:
            self.log.error("No article URLs found for %s.", self.name)
            return []

        articles = []

        for i, url in enumerate(urls, start=1):
            self.log.info("[%d/%d] %s", i, len(urls), url)
            article = self.scrape_article(url)
            if article:
                articles.append(article)
                self.log.info("    title: %s  paragraphs: %d", article["title"], len(article["content"]))
            else:
                self.log.warning("    skipped: %s", url)

            if i < len(urls):
                time.sleep(self.request_delay)

        with open(self.output_file, "w", encoding="utf-8") as f:
            json.dump(articles, f, ensure_ascii=False, indent=2)

        self.log.info("Saved %d articles to %s", len(articles), self.output_file)
        return articles