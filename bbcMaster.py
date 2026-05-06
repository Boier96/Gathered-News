import requests
import re
import json
from bs4 import BeautifulSoup

class Article:
  article_paragraphs = {}
  article_headers = {}
  article_title = ""

def createArticle(targeturl):
  article_html = requests.get(targeturl)
  article_soup = BeautifulSoup(article_html, 'html.parser')
  new_article = Article()
  new_article.article_headers = article_soup.find('article').find_all('h2')
  new_article.article_title = article_soup.find('article').find_all('h1')
  new_article.article_paragraphs = article_soup.find('article').find_all('p')
  return new_article

def getFormattedUrl(href_string, top_url):
  new_string = re.findall('"([^"]*)"', href_string)[0]
  if top_url not in new_string:
    new_string = top_url + new_string
  return new_string

def scrapetheBBC(targetFile):
  print("hello")
  target_file = targetFile
  url = "https://www.bbc.com/"
  full_html = requests.get(url)
  soup = BeautifulSoup(full_html, 'html.parser')

  article_area = soup.find(class_="sc-cd6075cf-0 cJhFtM")

  articles_finished = []

  article_links = article_area.find_all('a')
  for link in article_links:
    print(link.text)
    articles_finished.append(createArticle(getFormattedUrl(link.prettify(), "https://bbc.com")))
  for article in articles_finished:
    print(article.article_title)