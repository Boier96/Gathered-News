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
  new_article = Article
  new_article.article_headers = article_soup.find('article').find_all('h2')
  new_article.article_title = article_soup.find('article').find_all('h1')
  new_article.article_paragraphs = article_soup.find('article').find_all('p')
  return new_article

def getFormattedUrl(href_string, top_url):
  new_string = re.findall('"([^"]*)"', p)[0]
  if top_url not in new_string:
    new_string = top_url + new_string
  return new_string

def scrapetheBBC(targetFile):
  target_file = targetFile
  url = "https://www.bbc.com/"
  full_html = requests.get(url)
  soup = BeautifulSoup(full_html.text, 'html.parser')

  article_area = soup.find(class_="sc-cd6075cf-0 cJhFtM")

  article_links = article_area.find_all('a')
  for link in article_links:
    if "href" in link:
      createArticle(getFormattedUrl(link.text))




def GetTextFromList(list):
  finalString = "";
  for element in list:
    finalString = finalString + ", " + element
  return finalString

data = {
  "linkHTML": articleLinkHTML.prettify()
}

str_json = json.dumps(data, indent=4)
with open(targetfile, "w", encoding="utf-8") as f:
  f.write(str_json)

# getting the link to the full article

readtext = ""
soughtLinkAddition = ""

with open(targetfile, "r", encoding="utf-8") as f:
  readtext = f.readline()

readtextList = readtext.split()
for p in readtextList:
  if "href" in p:
    soughtLinkAddition = re.findall('"([^"]*)"', p)[0]

print(soughtLinkAddition)

articleUrl = soughtLinkAddition
if "bbc.com" not in soughtLinkAddition:
  articleUrl = "https://www.bbc.com" + soughtLinkAddition
# link to full article gotten

fullArticleResponse = requests.get(articleUrl)

articleSoup = BeautifulSoup(fullArticleResponse.text, 'html.parser')

articleText = []
articleParagraphs = articleSoup.find_all('p')
for p in articleParagraphs:
  articleText.append(p.text)

datasecond = {
  "articleText": GetTextFromList(articleText)
}

str_json_allegedly = json.dumps(datasecond, indent=4)
with open(targetfile, "w", encoding="utf-8") as f:
  f.write(str_json_allegedly)