import requests
import re
from bs4 import BeautifulSoup

targetfile = "result.txt"

url = "https://www.bbc.com/"
response = requests.get(url)

soup = BeautifulSoup(response.text, 'html.parser')

articlebox = soup.find(class_="sc-d8331fdf-2 dGLAfP")
articleLinkHTML = articlebox.find('a')

#articleText = []
#for p in articleParagraphs:
 # articleText.append(p.text)

firstHeadersText = []
firstHeaders = soup.find_all('p')
for header in firstHeaders:
  firstHeadersText.append(header.getText())

def GetTextFromList(list):
  finalString = "";
  for element in list:
    finalString = finalString + ", " + element
  return finalString

with open(targetfile, "w", encoding="utf-8") as f:
  f.write(articleLinkHTML.prettify())

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

articleUrl = "https://www.bbc.com" + soughtLinkAddition
# link to full article gotten

fullArticleResponse = requests.get(articleUrl)

articleSoup = BeautifulSoup(fullArticleResponse.text, 'html.parser')

articleText = []
articleParagraphs = articleSoup.find_all('p')
for p in articleParagraphs:
  articleText.append(p.text)

print(articleText)

with open(targetfile, "w", encoding="utf-8") as f:
  f.write(GetTextFromList(articleText))
