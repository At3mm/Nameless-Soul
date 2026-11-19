from pathlib import Path
import urllib.request,urllib.parse,http.cookiejar,re,json,html
from lxml import html as H
B=Path(__file__).parent
packs={
 'Skeleton':'https://idarklinkz.itch.io/ghostly-skeleton-free-sounds-asset',
 'Medieval':'https://heyheythere.itch.io/medieval-combat-sfx-free',
 'Werewolf':'https://ateliermagicae.itch.io/werewolf-sound-effects-vol-1',
 'MagicLite':'https://willosuna.itch.io/magic-spells-sfx-lite',
 'ClassicMonsters':'https://coucassi.itch.io/free-monster-sounds'}
for name,url in packs.items():
 try:
  opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
  opener.addheaders=[('User-Agent','Mozilla/5.0')]
  s=opener.open(url,timeout=40).read().decode('utf-8')
  token=html.unescape(re.search(r'name="csrf_token" value="([^"]+)"',s).group(1))
  req=urllib.request.Request(url+'/download_url',data=urllib.parse.urlencode({'csrf_token':token}).encode(),headers={'Referer':url,'X-Requested-With':'XMLHttpRequest'})
  answer=json.loads(opener.open(req,timeout=40).read())
  if not answer.get('url'):raise ValueError('No free download URL: '+str(list(answer)))
  page=opener.open(answer['url'],timeout=40).read().decode('utf-8')
  (B/(name+'-download.html')).write_text(page,encoding='utf-8')
  tree=H.fromstring(page);buttons=tree.xpath('//a[contains(@class,"download_btn")][@data-upload_id]')
  if len(buttons)!=1:raise ValueError('Expected exactly one free file')
  upload=buttons[0].get('data-upload_id')
  token=html.unescape(re.search(r'name="csrf_token" value="([^"]+)"',page).group(1))
  endpoint=url+'/file/'+upload+'?source=game_download'
  req=urllib.request.Request(endpoint,data=urllib.parse.urlencode({'csrf_token':token}).encode(),headers={'Referer':answer['url'],'X-Requested-With':'XMLHttpRequest'})
  fileinfo=json.loads(opener.open(req,timeout=40).read())
  if not fileinfo.get('url'):raise ValueError('Download response has no URL: '+str(list(fileinfo)))
  names=tree.xpath('//strong[contains(@class,"name")]/@title')
  filename=re.sub(r'[<>:"/\\|?*]', '_', names[0]) if names else name+'.zip'
  if name == 'MagicLite': filename='magic-spells-lite.zip'
  folder=B/name;folder.mkdir(exist_ok=True)
  content=opener.open(fileinfo['url'],timeout=60).read();(folder/filename).write_bytes(content)
  (folder/'SOURCE.txt').write_text('Source: '+url+'\nDownloaded: 2026-11-16\nOriginal archive: '+filename+'\n',encoding='utf-8')
  print(name,filename,len(content),'bytes')
 except Exception as ex:print(name,type(ex).__name__,str(ex)[:130])
bear=B/'Bear';bear.mkdir(exist_ok=True)
data=urllib.request.urlopen('https://opengameart.org/sites/default/files/bear.zip',timeout=40).read();(bear/'bear.zip').write_bytes(data)
(bear/'SOURCE.txt').write_text('Bear Growls — AntumDeluge, from U.S. Fish & Wildlife Service\nhttps://opengameart.org/content/bear-growls\nCC0\nDownloaded: 2026-11-16\n',encoding='utf-8')
print('Bear',len(data),'bytes')
