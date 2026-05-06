import requests
import re
import json
from bs4 import BeautifulSoup
from bs4 import XMLParsedAsHTMLWarning
import warnings
import logging

warnings.filterwarnings("ignore", category=XMLParsedAsHTMLWarning)

class Article:
  article_paragraphs = {}
  article_headers = {}
  article_title = ""

class ReturnURL:
  url = ""
  failed = False

def createArticle(targeturl):
  article_html = requests.get(targeturl, headers={'User-Agent': 'Custom'})
  try:
    article_html.raise_for_status()
    article_soup = BeautifulSoup(article_html.text, 'html.parser')
    new_article = Article()
    try:
      new_article.article_headers = article_soup.find_all('h2')
      new_article.article_title = article_soup.find_all('h1').text
      new_article.article_paragraphs = article_soup.find_all('p')
    except:
      print(article_soup.find('h1').text)

    return new_article
  except:
    try:
      article_soup = BeautifulSoup(article_html.text, 'lxml')
      new_article = Article()
      try:
        new_article.article_headers = article_soup.find_all('h2')
        new_article.article_title = article_soup.find_all('h1').text
        new_article.article_paragraphs = article_soup.find_all('p')
      except:
        print(article_soup.find('h1').text)

      return new_article
    except:
      print("returning shit for: " + targeturl)
      return Article()

def getFormattedUrl(href_string):
  result = ReturnURL()
  #print(href_string)
  pattern =  r'https?://\S+|www\.\S+'
  result.url = re.search(pattern, href_string)[0].replace('"', '')
  print(result.url)
  if "bbc.com" not in result.url:
    result.failed = True
    print("cooked")
    return result
  print("returning proper result with url:" + result.url)
  return result

def scrapetheBBC(targetFile):
  target_file = targetFile
  url = "https://www.bbc.com/"
  full_html = requests.get(url)
  soup = BeautifulSoup(full_html.text, 'html.parser')

  article_area = soup.find(class_="sc-9cd5bb24-0 cAWTFK")

  articles_finished = []

  article_links = article_area.find_all('a')

  for link in article_links:
    try:
      returnedUrl = getFormattedUrl(link.prettify) 
      if not returnedUrl.failed:
        print("gurt")
        articles_finished.append(createArticle(getFormattedUrl(link.prettify()).url))
    except:
      continue
  for article in articles_finished:
    print(article.article_title)