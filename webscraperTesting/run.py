import json
import logging
from bbc import BBCScraper
from aljazeera import AlJazeeraScraper
from dw import DWScraper
from cbc import CBCScraper
from guardian import GuardianScraper

log = logging.getLogger("runner")

OUTLETS = [
     BBCScraper,
     AlJazeeraScraper,
     DWScraper,
     CBCScraper,
     GuardianScraper,
]

COMBINED_OUTPUT = "all_articles.json"


def run_all():
    all_articles = []

    for ScraperClass in OUTLETS:
        scraper = ScraperClass()
        articles = scraper.run()
        all_articles.extend(articles)

    with open(COMBINED_OUTPUT, "w", encoding="utf-8") as f:
        json.dump(all_articles, f, ensure_ascii=False, indent=2)

    log.info("All scrapers finished. %d total articles written to %s.", len(all_articles), COMBINED_OUTPUT)
    return all_articles


if __name__ == "__main__":
    run_all()