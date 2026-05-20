# -*- coding: utf-8 -*-
from resources.scripts.common import *

# printing the value of unique MAC
try:
    resourcesPaht = xbmcvfs.translatePath("special://home/addons/plugin.video.seyirTURK/resources/scripts/")
except:
    resourcesPaht = xbmc.translatePath("special://home/addons/plugin.video.seyirTURK/resources/scripts/")
    
def get_parsers():
    modified_flag = False
    parsers_modified_date = settings.getSetting("parsers_modified_date")
    if not os.path.isfile(resourcesPaht +"parsers.py") and not os.path.isfile(resourcesPaht +"parsers.pyc")and not os.path.isfile(resourcesPaht +"parsers.pyo"):
        parsers = requests.get(root2 + "/kodi/parsers.py")
    else:
        parsers = requests.get(root2 + "/kodi/parsers.py", headers = {"if-Modified-since": parsers_modified_date})
        modified_flag = True
    page = parsers.text
    if "coding: utf-8" in page:
        parsers_version = re.findall('version="(.*?)"', page)[0]
        settings.setSetting("parsers_version", parsers_version)
        if modified_flag:
            parsers_modified_date = parsers.headers["last-modified"]
            settings.setSetting("parsers_modified_date",str(parsers_modified_date))
        try:
            with open(resourcesPaht +"parsers.py" ,"w", encoding="utf-8") as o:
                o.write(to_utf8(page.replace("\n","")))
        except:
            with codecs.open(resourcesPaht +"parsers.py", "w", "utf-8") as  o:
                o.write(page)
        try:
            d = py_compile.compile(resourcesPaht +"parsers.py", resourcesPaht +"parsers.pyc")
        except:
            pass
        try:
            e = py_compile.compile(resourcesPaht +"parsers.py", resourcesPaht +"parsers.pyo")
        except:
            showMessage("seyirTURK", "Kod Çözücü yüklenemedi!!!!")
        try:
            os.remove(resourcesPaht +"parsers.py")
        except:
            pass

try:
    from resources.scripts import parsers
except:
    pass

m_id = 0
playList = xbmc.PlayList(xbmc.PLAYLIST_VIDEO)

try:
    reload(sys)
    sys.setdefaultencoding('utf-8')
except:
    pass

settings = xbmcaddon.Addon(id='plugin.video.seyirTURK')

class MyPlayer(xbmc.Player):

    def __init__( self, *args, **kwargs ):
        xbmc.Player.__init__( self )
        self.isfirst = 1
        self.curPos = 0
        self.hasSaved = False
        self.lang_flag = "0"
        self.user_id = 0
        self.media_path = None
        self.playbackend = None
        try:
            self.user_id = int(settings.getSetting( "user_id" ))
        except: 
            pass

    def newplay(self, playlist, m_id, main_url, isTv="0",lang= -1, media_type = "video_playlist"):
        self.playlist = playlist
        # self.media_path = playlist[1].getPath()
        # showMessage(1,self.media_path)
        self.play(self.playlist)
        self.m_id = m_id
        self.lang = lang
        self.main_url = main_url
        self.isTv = isTv
        self.timestamp = 0
        self.media_type = media_type
        try:
            if self.isTv == '0':
                self.timestamp  = int(fetch(root + 'save.php?type=g&m_id=' + str(self.m_id)))/1000
            elif self.isTv == '1':
                self.timestamp  = int(fetch(root + 'save.php?type=g&isTv=1&m_id=' + str(self.m_id)))/1000
        except:
            try:
                time.sleep(1)
                if self.isTv == '0':
                    self.timestamp  = int(fetch(root + 'save.php?type=g&m_id=' + str(self.m_id)))/1000
                elif self.isTv == '1':
                    self.timestamp  = int(fetch(root + 'save.php?type=g&isTv=1&m_id=' + str(self.m_id)))/1000
            except:
                pass
        if self.timestamp > 0 and isTv != "notmovie|tvseries":
            if url is not None:
                key = dialog.yesno('[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]', '\nVideo nereden başlatılsın?', yeslabel='Baştan', nolabel='Kaldığım Yerden')
                if key ==1:
                    self.timestamp=0
                else:
                    pass
        while self.isPlaying():
            xbmc.sleep(10)
            try:
                self.curPos = int(self.getTime())
            except:
                a=1
            try:
                self.total = int(self.getTotalTime())
            except:
                pass
            # if self.total > 0 and int((self.curPos/self.total)*100) == 80 :
            #     self.next_play()    

    def __del__(self) :
        if self.user_id != 0 and self.curPos > 100 and self.m_id !=0 :
            if self.isTv != "notmovie|tvseries":
                self.save()
    
    def save(self):
        self.mili = self.curPos * 1000
        try:
            self.toplam = self.total * 1000
            self.percent = 100*self.mili/self.toplam
        except:
            self.percent = 0
        bit = 90
        if self.isTv == "1":
            bit = 85
        if self.percent > bit:
            self.isDone = '1'
        else:
            self.isDone = '0'
        if  self.isTv == "0":
            temp_watched = settings.getSetting('temp_watched')
            if str(self.m_id) not in temp_watched:
                temp_watched += "," + str(self.m_id)
                settings.setSetting("temp_watched",temp_watched)
            self.ur = root + "save.php?type=s&m_id=" + str(self.m_id) +"&mili=" + str(self.mili)
        else:
            self.ur = root + "save.php?isTv=1&type=s&&m_id=" + str(self.m_id) +"&mili=" + str(self.mili) + '&isDone=' + self.isDone
        if not self.hasSaved  and isTv != "notmovie|tvseries":
            page = fetch(self.ur)
            self.hasSaved = True
            if self.isDone == '1' and self.isTv == "1" :
                autoplay(self.m_id, self.isTv, self.main_url, self.lang_flag)
            # elif self.isDone != "1" and self.isTv =="1":
            #     settings.setSetting("autoplay_last_subsource","")
            #     xbmc.executebuiltin("Action(Back)")
            #     xbmc.executebuiltin('Container.Update')
            else:
                settings.setSetting("autoplay_last_subsource","")

    # def next_play(self):
    #     # current_item = self.getPlayingItem()
    #     # current_position = 0
    #     # if current_item is not None:
    #     #     for idx in range(len(self.playlist)):
    #     #         if self.playlist[idx].getPath() == current_item.getPath():
    #     #             current_position = idx
    #     #             break
    #     current_position = self.playlist.getposition()
    #     while True:
    #         self.next_idx = current_position + 1
    #         # showMessage(self.next_idx, len(self.playlist))
    #         next_item = self.playlist[self.next_idx]
    #         # playlist = xbmc.PlayList(xbmc.PLAYLIST_VIDEO)
    #         self.playlist.remove(self.playlist[self.next_idx].getPath())
    #         link = parsers.youtube(next_item.getPath())
    #         if link is not None and link.startswith("http"):
    #             self.playlist.add(link, next_item, self.next_idx)
    #             break
    #         else:
    #             pass

    if ver() >= 18:
        def onAVStarted(self):
            xbmc.executebuiltin("Dialog.Close(busydialog)")

            # if self.media_type == "video_playlist":
            #     try:
            #         self.next_play()
            #     except:
            #         pass
            if self.isfirst == 1 and isTv != "notmovie|tvseries":
                self.isfirst = 0
                if self.timestamp  !=0 :
                    self.seekTime(self.timestamp )
            self.langs = self.getAvailableAudioStreams()
            try:
                if self.lang == 1:
                    self.audiostream = int([i for i, elem in enumerate(self.langs) if 'en' in elem][0])
                elif self.lang == 0:
                    self.audiostream = int([i for i, elem in enumerate(self.langs) if 'tr' in elem][0])
                self.setAudioStream(self.audiostream)
            except:
                pass
            while self.isPlaying and self.isTv == "notmovie|tvseries" and self.playbackend is None:
                xbmc.sleep(10)

    else:
        def onPlayBackStarted(self):
            xbmc.executebuiltin("Dialog.Close(busydialog)")
            # try:
            #     self.next_play()
            # except:
            #     pass
            if self.isfirst == 1 and isTv != "notmovie|tvseries":
                self.isfirst = 0
                if self.timestamp  !=0 :
                    self.seekTime(self.timestamp )
            self.langs = self.getAvailableAudioStreams()
            try:
                if self.lang == 1:
                    self.audiostream = int([i for i, elem in enumerate(self.langs) if 'en' in elem][0])
                elif self.lang == 0:
                    self.audiostream = int([i for i, elem in enumerate(self.langs) if 'tr' in elem][0])
                self.setAudioStream(self.audiostream)
            except:
                pass
            while self.isPlaying and self.isTv == "notmovie|tvseries" and self.playbackend is None:
                xbmc.sleep(10)
                
    def onPlayBackStopped(self):
        self.playbackend = True
        
    def onPlayBackEnded(self):
        if self.next_idx == len(self.playlist):
            showMessage("ended")
            self.playbackend = True
        
    def onPlayBackError(self):
        self.playbackend = True
i = 1
while osInfo == xbmc.getLocalizedString(503).encode("utf8"):
    i = i+1
    osInfo = xbmc.getInfoLabel('System.OSVersionInfo') 
    time.sleep(1)
    if i == 10:
        break
    
if not settings.getSetting( "recorded_date") or settings.getSetting('recorded_date') == "01-01-2020":
    settings.setSetting('recorded_date', xbmc.getInfoLabel('System.Date(dd-mm-yyyy)'))
    
dialog = xbmcgui.Dialog()
xbmc.sleep(1000)
if settings.getSetting('uclugorunum') == "true":
    xbmcplugin.setContent(int(sys.argv[1]), 'movies')
tc = hashlib.md5(vidName.encode()).hexdigest()

def rootcheck():
    global root2
    global root
    i = 0
    old = settings.getSetting('root')
    while True:
        ek = str(i)
        if i == 0 : ek = ""
        root = decode_base64((fetch(decode_base64("aHR0cHM6Ly9zZXlpcnR1cmsubmV0L3Jvb3RjaGVjay8=") + ek + "kodi.php"))[::-1]) + "sey/back/"
        # root = "https://tinyurl.com/29wyfykr/sey/back/"
        # root = "https://cekke.cfd/sey/back/"
        settings.setSetting('root', root)
        root2 = '/'.join(root.split('/')[:-2])
        break
        i += 1
    if root != old:
        cache_clear()
        settings.setSetting("temp_watched","mids")
        
def macaddress():
    try:
        if os.path.exists("/usr/lib/enigma2/python/Plugins/Extensions/KodiLite"):
            try:
                from Components.Network import iNetwork
                ifaces = iNetwork.getConfiguredAdapters()
                mac_id = iNetwork.getAdapterAttribute(ifaces[0], 'mac')
                settings.setSetting('mac_add', mac_id)
            except:
                pass
        else:
            mac_id = xbmc.getInfoLabel('Network.MacAddress')
            i = 1
            while mac_id == xbmc.getLocalizedString(503).encode("utf8"):
                i = i+1
                mac_id = xbmc.getInfoLabel('Network.MacAddress') 
                time.sleep(1)
                if i == 10:
                    break
            if  not ('gul' in mac_id or 'usy' in mac_id or mac_id == "" or mac_id == " " or "Occup" in mac_id or "Zaj" in mac_id or 'Besch' in mac_id or 'Bezig' in mac_id):
                settings.setSetting('mac_add', mac_id)
            else:
                settings.setSetting('mac_add', '00:00:00:00:00:00')
    except:
        settings.setSetting('mac_add', '00:00:00:00:00:00')

if not settings.getSetting('mac_add') or settings.getSetting('mac_add')=="" or settings.getSetting('mac_add')==" " or "Occup" in  settings.getSetting('mac_add') or "Zaj" in settings.getSetting('mac_add') or 'gul' in  settings.getSetting('mac_add') or 'usy' in  settings.getSetting('mac_add') or '00:00:00:00:00' in  settings.getSetting('mac_add') or 'Besch' in  settings.getSetting('mac_add') or 'Bezig' in  settings.getSetting('mac_add'):
        macaddress()

def message():
    temp = '/storage/emulated/0/Android/2711020519199468'
    temp1 = translatepath(os.path.join("special://home/system/",'2711020519199468'))
    if os.path.exists(temp) or os.path.exists(temp1):
        settings.setSetting("vidid", "1")
    try:
        surum = settings.getAddonInfo('version')
        mesaj_page = fetch(root2 + '/mesaj.php?surum=' + surum + "&date=" + xbmc.getInfoLabel('System.Date(yyyy-mm-dd)'))
        mesaj_json = json.loads(mesaj_page)
        update_text = mesaj_json["message"][0]["surum"]
        if 'eski_surum' in update_text:
            update(update_text)  
        mesaj = mesaj_json["message"][0]["mesaj"]
        u_mesaj = mesaj_json["message"][0]["u_mesaj"]
        flag = mesaj_json["message"][0]["flag"]
        u_flag = mesaj_json["message"][0]["u_flag"]
        is_user_active = mesaj_json["message"][0]["isUserActive"]
        if is_user_active == 0:
            settings.setSetting('mail', '')
            settings.setSetting('sifre', '')
            settings.setSetting('e_mail', '')
            settings.setSetting('user_id', '')
        if settings.getSetting("message") != flag:
            ok1 = dialog.ok("[COLOR orange][B]seyirTURK Kodi[/B][/COLOR] Mesajınız var !", mesaj)
            settings.setSetting('message', flag)
        if settings.getSetting("u_message") != u_flag and u_flag != '' and u_mesaj != '':
            ok1 = dialog.ok("[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]", u_mesaj)
            settings.setSetting('u_message', u_flag)
        cache_key = mesaj_json["message"][0]["cache_key"]
        if  cache_key != settings.getSetting("cache_key_local"):
            cache_clear()
            settings.setSetting("temp_watched","mids")
        settings.setSetting("cache_key_local",cache_key)
        GUA = json.loads(requests.get("https://www.useragents.me/api").text)["data"][random.randint(0,44)]["ua"]
        settings.setSetting("GUA",GUA)
    except:
        pass
    try:
        day = str(datetime.date.today().day)
        if  day != settings.getSetting("day"):
            settings.setSetting("day", day)
    except:
        pass

    
def save_m3u_link():
    if 'http' in settings.getSetting('m3u'):
        try:
            v = fetch(settings.getSetting('m3u'))
            if '#EXTINF' in v:
                with codecs.open(translatepath(os.path.join(DATA_PATH,"gecici.m3u")), "w+", "utf-8-sig") as out:
                    out.write(to_utf8(v))
        except:
            pass

def Basla():
    try:
        xbmcvfs.mkdir(translatepath(os.path.join(temp,'86519')))
    except:
        pass
    try:
        rootcheck()
    except:
        pass
    try:
        with codecs.open(translatepath(os.path.join(ADDON_PATH, vidName + '.py')), "r", "utf-8-sig") as out:
            vv = out.read()
        check_teng = re.findall('#(###\s*tengildet\s*cop)y', vv)
    except:
        check_teng = False
    try:
        if check_teng:
            #showMessage("seyirTURK","Parser indirilmedi.")
            pass
        else:            
            get_parsers()
    except:
        pass
    threading.Thread(target=message).start()
    threading.Thread(target=mem_cont).start()
    threading.Thread(target=epg,args=(settings,1,)).start()
    threading.Thread(target=save_m3u_link).start()
    main()

def ayarlar():
    settings.openSettings()

def main():
    url = root2 + '/kodi/main.php?ct=' + tc + "&surum=" + settings.getAddonInfo('version')
    page = cache(url)
    if page != "":
        jr = json.loads(page)
        for rj in jr["main"]:
            link = rj["link"]
            resim = rj["icon"]
            isim = rj["title"]
            try:
                fanart = rj["Backdrop"]
            except:
                fanart = resim
            try:
                desc = rj["Summary"]
            except:
                desc=""
            sign ='?'
            if '?' in link:
                sign = '&'
            link = link   + sign + 'ct=' + tc
            if '&id=' in link:
                link = link.replace('&id=','') + '&id='
            if '/iptv.php' in link: 
                link = link
            if settings.getSetting('isAdult') == "Rabbit" and settings.getSetting( "user_id" ) :
                addDir('[COLOR orange][B][COLOR blue]> [/COLOR]'+isim+'[/B][/COLOR]',Quote_plus(link),2,resim, 0, desc, fanart)
            else:
                if not 'Adult' in isim:
                    if "sources_test" in link:
                        addDir('[COLOR orange][B][COLOR blue]> [/COLOR]Kaynak Testleri[/B][/COLOR]',Quote_plus(link),5,resim, 0, desc,fanart, 0, "kaynaktestleri")
                    else:
                        addDir('[COLOR orange][B][COLOR blue]> [/COLOR]'+isim+'[/B][/COLOR]',Quote_plus(link),2,resim, 0, desc,fanart)

        if settings.getSetting('m3u'):
            if 'type=m3u'in settings.getSetting('m3u') or '.m3u'in settings.getSetting('m3u') :
                linkos = Quote(settings.getSetting('m3u'))
                desc = '[COLOR orange][B]seyirTURK[/B][/COLOR] te kendi kendi IPTV lerinizi bu alanda bulabilirsiniz.'
                addDir('[COLOR orange][B][COLOR blue]> [/COLOR]Benim Iptv[/B][/COLOR]',linkos,2,os.path.join(IMAGES_PATH, 'myiptv.png'),0, desc,fanart)
                
        desc = '[COLOR orange][B]seyirTURK[/B][/COLOR] hakkımızdaki bilgileri görebileceğiniz alan.'
        addDir('[COLOR orange][B][COLOR blue]> [/COLOR]Bilgiler[/B][/COLOR]','bilgiler.php',5,os.path.join(IMAGES_PATH, 'info2.png'),0, desc, fanart, "0", "bilgiler")

        desc = '[COLOR orange][B]seyirTURK[/B][/COLOR] ayarlarını yapabileceğiniz alan.'
        addDir('[COLOR orange][B][COLOR blue]> [/COLOR]Ayarlar[/B][/COLOR]','main.php',5,os.path.join(IMAGES_PATH, 'settings.png'),0, desc, fanart,"0","ayarlar")


    
def listele(url):
        # import locale
        # locale.setlocale(locale.LC_ALL, 'turkish')
        kokd = root
        isSearchNegative = 0
        searchstring = ""
        search_history = settings.getSetting("search_history")
        if search_history == "": search_terms = []
        else: search_terms = search_history.split("#")
        last_array = list(set(search_terms))
        # last_array.sort(key=locale.strxfrm)
        last_array = list(set(sorted(search_terms)))
        new_search_flag = False
        clear_history_flag = False
        try:
            searchstring = xbmcgui.Window(10000).getProperty('searchstring')
        except:
            pass
        if ("?name" in url or '&name' in url) and not ('isAdult=1&name=' in url or 'iptv/search.php?name=' in url):
                key = dialog.select('[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]', ['[B]Hepsi[/B]', '[B]Türkçe Dublaj[/B]', '[B]Türkçe Altyazı[/B]', '[B]Almanca Dublaj[/B]', '[B]Kişi Arama[/B]'])
                if key != -1:
                    if key== 0:
                        substring = ''
                    elif key == 1:
                        substring = '&lang=DUB'
                    elif key == 2:
                        substring = '&lang=SUB'
                    elif key == 3:
                        substring = '&lang=GER'
                    else:
                        substring = ''
                        url = url.replace("p_type=Movie","p_type=person").replace("p_type=TV","p_type=person")
                    
                    if settings.getSetting("is_search_history") == "true":
                        if len(last_array) >= 1:
                            try:
                                last_array.pop(last_array.index("[COLOR lightcoral]Geçmişi Sil[/COLOR]"))
                            except:
                                pass
                            # last_array.sort(key=locale.strxfrm)
                            last_array = sorted(last_array)
                            last_array.insert(0, "[COLOR lightcoral]Geçmişi Sil[/COLOR]")
                            key_history = dialog.select('[B]Arama Geçmişi[/B]', last_array)
                            if key_history != -1 and key_history != 0:
                                searchstring = last_array[key_history]
                                xbmcgui.Window(10000).setProperty('searchstring', searchstring)
                                url = url.replace('name=', 'name=' + Quote(searchstring) + substring)
                            elif key_history == 0:
                                settings.setSetting("search_history","")
                                clear_history_flag = True
                            else:
                                new_search_flag = True
                        else:
                            new_search_flag = True
                    else:
                        new_search_flag = True

                    if new_search_flag:
                        keyboard = xbmc.Keyboard(searchstring, "Arama", False )
                        keyboard.doModal()
                        if (keyboard.isConfirmed()):
                                searchstring = keyboard.getText().strip()
                                xbmcgui.Window(10000).setProperty('searchstring', searchstring)
                                last_array.append(searchstring)
                                new_value_of_history = "#".join(list(set(last_array)))
                                settings.setSetting("search_history", new_value_of_history)
                                if key== 0:
                                    substring = ''
                                elif key == 1:
                                    substring = '&lang=DUB'
                                elif key == 2:
                                    substring = '&lang=SUB'
                                elif key == 3:
                                    substring = '&lang=GER'
                                else:
                                    substring = ''
                                    url = url.replace("p_type=Movie","p_type=person").replace("p_type=TV","p_type=person")
                                url = url.replace('name=', 'name=' + Quote(searchstring) + substring)
                        else:
                            isSearchNegative = 1

                else:
                    isSearchNegative = 1
        elif 'isAdult=1&name=' in url or 'iptv/search.php?name=' in url:
            searchstring = ""
            try:
                searchstring = xbmcgui.Window(10000).getProperty('searchstring_adult')
            except:
                pass            
            keyboard = xbmc.Keyboard(searchstring, "Arama", False )
            keyboard.doModal()
            if (keyboard.isConfirmed() ):
                searchstring = keyboard.getText().strip().replace(" ", "%20")
                xbmcgui.Window(10000).setProperty('searchstring_adult', searchstring)
                url = url.replace('name=', 'name=' + searchstring)
            else:
                isSearchNegative = 1
            
            
        if 'type=user' in url or 'type=history' in url:
            if settings.getSetting( "user_id" ):
                url = url + settings.getSetting( "user_id" )
            else :
                ok = dialog.ok("[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]","Girmek istediğiniz yer için lütfen ayarlardan kullanıcı girişi yapınız.")
                url = "bos"

        else :
            pass
        if "seyirturkelkitabi" in url:
            try:
                webbrowser.open("https://seyirturk.net/kullanici-el-kitabi/")
            except:
                showMessage("[COLOR orange][B]seyirTURK[/B][/COLOR]","[COLOR orange][B]Cihazınızda herhangi bir internet tarayıcısı yok![/B][/COLOR]")
            url = "bos"
        
        if url!="bos" :
            if "adult" in url or "%2b18" in url or "erotik" in url or "yetiskin" in url or "Erotik" in url or "Yetiskin" in url:
                if settings.getSetting( "isAdult" ) == "Rabbit":			
                    k = xbmc.Keyboard('', 'Yetişkin Şifresini Giriniz') ; k.doModal()
                    pin = k.getText()
                    if k.isConfirmed():
                        if pin != settings.getSetting( "isAdultkey" ):
                            isSearchNegative = -1
                            url='/'.join(kokd.split('/')[:-2]) + '/kodi/main.php'
                    else:
                        isSearchNegative = 1
                        url='/'.join(kokd.split('/')[:-2]) + '/kodi/main.php'
            else:
                pass
            if url.startswith('http'):
                if settings.getSetting('m3u') == url:
                    try:
                        url11 = os.path.join(DATA_PATH,'gecici.m3u' )
                        cc = open(url11,'r')
                        data1 = cc.read()
                        cc.close()
                        f = data1
                    except:
                        showMessage("[COLOR orange][B]seyirTURK[/B][/COLOR]","[COLOR orange][B]Girdiğiniz linkte m3u bulunamadı![/B][/COLOR]")
                        f = ''
                else:
                    f = cache(url)
                    try:
                        if "search." in url:
                            a = fetch(kokd + "dummy.php")
                    except:
                        pass
                    if "0 Games" in f:
                        showMessage("[COLOR orange][B]seyirTURK KODI[/B][/COLOR]", "Listelenecek medya yok!")
            else:
                if "bilgiler.php" not in url:
                    url1 = os.path.join(DATA_PATH, settings.getSetting('m3u') )
                    c = open(url1,'r')
                    data = c.read()
                    c.close()
                    f = data
                else:
                    f=''
            if '"movies":' in f and not 'm3u' in url:
                    prov_flag = False
                    try:
                        prov_for_filter = re.findall('provider_(.*?)&', url)[0]
                        settings.setSetting("prov", prov_for_filter)
                        if prov_for_filter != "fullhdfilmizlesene":
                            prov_flag = True
                    except:
                        settings.setSetting("prov", "")
                        
                    js = json.loads(f)
                    idx = '0'
                    if 'iptv.php' in url and not 'Adultkodiiptv.php' in url:
                        link = root2 + '/kodi/iptv/search.php?name='
                        addDir('[COLOR orange][B][COLOR blue]> [/COLOR] IPTV Arama [/B][/COLOR]',Quote_plus(link),2,os.path.join(IMAGES_PATH, 'ara.png'),'0', 'IPTV ler arasında arama yapabilirsiniz.')
                    for rs in js['movies']:
                            language = ''
                            son_ek = ''
                            try:
                                hw = int(rs['hasWatched'])
                            except:
                                hw = 0
                            if str(rs['ID']) in settings.getSetting("temp_watched") or hw > 0:
                                son_ek = "[COLOR white] - İzlendi[/COLOR]"
                            
                                
                            try:
                                language_int = rs['Language']
                                if settings.getSetting("ykge") != "true":
                                    
                                    che = int(rs['Language'])
                                    if ( che == 4 or che == 5 or che == 6):
                                        language_int = str(che - 4)
                                    elif (che == 8 or che == 9 or che == 10):
                                        language_int = str(che - 8)
                                    elif (che == 12 or che == 13 or che == 14):
                                        language_int = str(che - 12)                                 
                                
                                if language_int == "14":
                                    language = "[COLOR deepskyblue] TA - TD - GE - YK[/COLOR]" + son_ek
                                elif language_int == "13":
                                    language = "[COLOR deepskyblue] TD - GE - YK[/COLOR]" + son_ek
                                elif language_int == "12":
                                    language = "[COLOR mediumspringgreen] TA - GE - YK[/COLOR]" + son_ek
                                elif language_int == "11":
                                    language = "[COLOR gold] GE - YK[/COLOR]" + son_ek
                                elif language_int == "10":
                                    language = "[COLOR deepskyblue] TA - TD - YK[/COLOR]" + son_ek
                                elif language_int == "9" :
                                    language = "[COLOR deepskyblue] TD - YK[/COLOR]" + son_ek
                                elif language_int == "8":
                                    language = "[COLOR mediumspringgreen] TA - YK[/COLOR]" + son_ek
                                elif language_int == "7":
                                    language = "[COLOR tomato] YK[/COLOR]" + son_ek
                                elif language_int == "6":
                                    language = "[COLOR deepskyblue] TA - TD - GE[/COLOR]" + son_ek
                                elif language_int == "5":
                                    language = "[COLOR deepskyblue] TD - GE[/COLOR]" + son_ek
                                elif language_int == "4":
                                    language = "[COLOR mediumspringgreen] TA - GE[/COLOR]" + son_ek
                                elif language_int == "3":
                                    language = "[COLOR gold] GE[/COLOR]" + son_ek
                                elif language_int == "2" or language_int == "-1":
                                    language = "[COLOR deepskyblue] TA - TD[/COLOR]" + son_ek
                                elif language_int == "1":
                                    language = "[COLOR deepskyblue] TD[/COLOR]" + son_ek
                                elif language_int == "0":
                                    language = "[COLOR mediumspringgreen] TA[/COLOR]" + son_ek
                            except:
                                pass
                            baslik = rs['Name'] + language
                            if prov_flag:  ## siteler seçildiğinde 4K olmayan sitelerde +K filmlerin işareti kaldırılıyor.
                                baslik = baslik.replace("-(4K)","")
                            try:
                                if 'Erotik' in rs["Genres"]:
                                    baslik = rs['Name']
                            except:
                                pass
                            resim = rs['Image']
                            try:
                                fanart = rs["Backdrop"]
                            except:
                                fanart = resim
                            try:
                                imdbscore = rs["IMDBScore"]
                            except:
                                imdbscore = 'NA'
                            try:
                                releasedate = rs["ReleaseDate"]
                                if "1900" in releasedate:
                                    releasedate = 'Bulunamadı'
                            except:
                                releasedate = 'Bulunamadı'
                            try:
                                runingh = '\nSüre: ' + rs["Runtime"] + ' dk.'
                            except:
                                runingh = ''
                            try:
                                genres = rs["Genres"]
                            except:
                                genres = 'NA'
                            try:
                                try:
                                    if settings.getSetting('uclugorunum') == "true":
                                        desc = '[COLOR green][B]IMDb: ' + imdbscore +'[/COLOR][COLOR blue] Tarih: ' + releasedate.replace(' 00:00:00','') + '[/COLOR][COLOR grey]' + runingh + '[/COLOR][/B]\n[COLOR yellow]Türler: ' + genres + '[/COLOR]\n' + rs['Summary']
                                        if  'turler.php' in url or 'turlerdizi.php' in url or 'diziler.php' in url or 'filmler.php' in url :
                                            desc = rs['Summary'] 
                                    else:
                                        desc = '[COLOR orange][B]' + (baslik.replace(' TD','').replace(' TA','').replace(' YK','').replace(" GE","").replace("-","")).strip() + '[/B][/COLOR]' + '\n' +'[COLOR green][B]IMDb: ' + imdbscore +'[/COLOR][COLOR blue] Tarih: ' + releasedate.replace(' 00:00:00','') + '[/COLOR][/B]\n[COLOR yellow]Türler: ' + genres + '[/COLOR]\n' + rs['Summary']
                                        if  'turler.php' in url or 'turlerdizi.php' in url or 'diziler.php' in url or 'filmler.php' in url :
                                            desc = '[COLOR orange][B]' + (baslik.replace(' TD','').replace(' TA','').replace(' YK','').replace(" GE","").replace("-","")).strip() + '[/B][/COLOR]' + '\n' + rs['Summary']  
                                            
                                except:
                                    desc = None
                                try:
                                    idx = str(rs['ID'])
                                except:
                                    pass
                                try :
                                    tip = rs["Type"]
                                except:
                                    tip ='Yok'
                                try:
                                    sign ='?'
                                    if '?' in rs['Link']:
                                        sign = '&'
                                    link = rs['Link']  + sign + 'ct=' + tc
                                except:
                                    if tip == 'Movie' or tip == 'yok':
                                        link = kokd + 'streams.php?id=' + idx
                                    else:
                                        link = kokd + 'episodes.php?id=' + idx
                                
                                if hw <= 0 or (settings.getSetting("check_haswatched") == "true" and hw > 0) or "genre=watched" in url:
                                    if 'show.php?type=user' in url:
                                        addDir('[COLOR orange][B][COLOR blue]# [/COLOR]'+ baslik +'[/B][/COLOR]',Quote_plus(link),2,resim, idx, desc)

                                    else:
                                        if not 'type=random' in url:
                                            isAdult = 0
                                            try:
                                                if rs["isAdult"] == "1":
                                                    isAdult = 1
                                            except:
                                                pass
                                            if isAdult != 1 and 'Adult' not in url:
                                                korean = settings.getSetting("uzakdogu")
                                                anim = settings.getSetting("anime")
                                                hint = settings.getSetting("hint")
                                                if anim == "false" and korean == "false" and hint == "false":
                                                    if "genre=korean" not in url and "genre=Anime" not in url and "genre=Indian" not in url:
                                                        if "Kore" not in genres and "Anime" not in genres and "Hint" not in genres:
                                                            addDir('[COLOR orange][B][COLOR blue]* [/COLOR]'+ baslik +'[/B][/COLOR]',Quote_plus(link),2,resim,idx, desc, fanart)            
                                                    if "genre=Anime" in url:
                                                        if "Kore" not in genres and "Hint" not in genres:
                                                            addDir('[COLOR orange][B][COLOR blue]* [/COLOR]'+ baslik +'[/B][/COLOR]',Quote_plus(link),2,resim,idx, desc, fanart)            
                                                    if "genre=korean" in url:
                                                        if "Anime" not in genres and "Hint" not in genres:
                                                            addDir('[COLOR orange][B][COLOR blue]* [/COLOR]'+ baslik +'[/B][/COLOR]',Quote_plus(link),2,resim,idx, desc, fanart)         
                                                    if "genre=Indian" in url:
                                                        if "Anime" not in genres and "Kore" not in genres:
                                                            addDir('[COLOR orange][B][COLOR blue]* [/COLOR]'+ baslik +'[/B][/COLOR]',Quote_plus(link),2,resim,idx, desc, fanart)       
                                                elif korean == "false" and "genre=korean" not in url:
                                                    if "Kore" not in genres:
                                                        addDir('[COLOR orange][B][COLOR blue]* [/COLOR]'+ baslik +'[/B][/COLOR]',Quote_plus(link),2,resim,idx, desc, fanart)            
                                                elif anim == "false" and "genre=Anime" not in url:
                                                    if "Anime" not in genres:
                                                        addDir('[COLOR orange][B][COLOR blue]* [/COLOR]'+ baslik +'[/B][/COLOR]',Quote_plus(link),2,resim,idx, desc, fanart)       
                                                elif hint == "false" and "genre=Indian" not in url:
                                                    if "Hint" not in genres:
                                                        addDir('[COLOR orange][B][COLOR blue]* [/COLOR]'+ baslik +'[/B][/COLOR]',Quote_plus(link),2,resim,idx, desc, fanart)           
                                                else:
                                                    addDir('[COLOR orange][B][COLOR blue]* [/COLOR]'+ baslik +'[/B][/COLOR]',Quote_plus(link),2,resim,idx, desc, fanart)            
                                            
                                            if settings.getSetting('isAdult') == "Rabbit" and settings.getSetting( "user_id" ) and 'Adult' in url:
                                                if "mainprovider" in url:
                                                    addDir('[COLOR orange][B][COLOR blue]{f} [/COLOR]'+ baslik + son_ek +'[/B][/COLOR]',Quote_plus(link),2,resim,idx, desc, fanart)
                                                elif "Adultfilmler.php" in url:
                                                    addDir('[COLOR orange][B][COLOR blue]{} [/COLOR]'+ baslik +'[/B][/COLOR]',Quote_plus(link),2,resim,idx, desc, fanart)
                                                elif "iptvmain_adult.png" in f:
                                                    addDir('[COLOR orange][B][COLOR blue]{tv} [/COLOR]'+ baslik +'[/B][/COLOR]',Quote_plus(link),2,resim,idx, desc, fanart)
                                                else:
                                                    addDir('[COLOR orange][B][COLOR blue]{c} [/COLOR]'+ baslik +'[/B][/COLOR]',Quote_plus(link),2,resim,idx, desc, fanart)
                                        else:
                                            listele(link)
                            except:
                                pass 
                    try:
                        if 'type=genre&genre' in url:
                            x = re.findall('start=(.*?)&', url)
                            genre = re.findall('genre&genre=(.*?)&',url)
                            data = int(x[0]) + 500
                            if not 'p_type=TV' in url:
                                url = Quote_plus(kokd + 'show.php?type=genre&genre=' + genre[0] + '&start='+str(data)+'&size=500')
                            else:
                                url = Quote_plus(kokd + 'show.php?type=genre&genre=' + genre[0] + '&p_type=TV&start='+str(data)+'&size=500')
                            if len(js['movies']) > 450:
                                addDir('[COLOR blue][B][COLOR blue]=> [/COLOR]Sonraki Sayfa[/B][/COLOR]',url,2,os.path.join(IMAGES_PATH, 'next.png'), idx, 'Sonraki Sayfa')

                        elif 'p_type=TV' in url or 'type=en_tv' in url or 'type=tr_tv' in url:
                            x = re.findall('start=(.*?)&',url)
                            data = int(x[0]) + 500
                            if 'p_type=TV' in url:
                                url = Quote_plus(kokd + 'show.php?type=genre&p_type=TV&start=' + str(data) + '&size=500&genre=all')
                            elif 'type=en_tv' in url:
                                url = Quote_plus(kokd + 'show.php?type=genre&start=' + str(data) + '&size=500&type=en_tv')
                            elif 'type=tr_tv' in url:
                                url = Quote_plus(kokd + 'show.php?type=genre&start=' + str(data) + '&size=500&type=tr_tv')
                            if len(js['movies']) > 450:
                                addDir('[COLOR blue][B][COLOR blue]=> [/COLOR]Sonraki Sayfa[/B][/COLOR]',url,2,os.path.join(IMAGES_PATH, 'next.png'), idx, 'Sonraki Sayfa')
                    except:
                        a=1
            elif "links" in f:
                try:
                    m_id = re.findall('id=([0-9]+)',url)[0]
                except:
                    m_id = None
                try:
                    ee_id = re.findall('&e_id=([0-9]+)',url)[0]
                except:
                    ee_id = 'yok'

                jr = json.loads(f)
                flag_any_link = False
                flag_iptv_radio = False
                if jr.get("isIPTV") is None and jr.get("isRADIO") is None and "Adult" not in url:
                    try:  jr["links"] = sorted(jr["links"], key=lambda k: k["Provider"])
                    except: pass
                for i, rj in enumerate(jr["links"]):
                    try:
                        isForeign = rj["isForeign"]
                    except:
                        isForeign = "0"
                    link = Quote(rj["Link"].encode('UTF-8'))
                    try:
                        e_id = rj["E_ID"]
                    except:
                        e_id = "0"
                    releasedate = 'NA'
                    name_from_labelinfo = ((xbmc.getInfoLabel('ListItem.Title').replace("* ","").replace(' TD','').replace(' TA','').replace(' YK','').replace(" GE","").replace("-","").replace("(4K)","") ).encode('utf-8').decode('utf-8')).strip()
                    if "rastgele" in name_from_labelinfo.lower():
                        name_from_labelinfo = '[COLOR orange][B]' + rj["name"] + '[/B][/COLOR]'
                    if name_from_labelinfo != "":
                        settings.setSetting("gecici_isim", name_from_labelinfo)
                    if name_from_labelinfo == "":
                        name_from_labelinfo = settings.getSetting("gecici_isim")
                    try:
                        provider = rj["Provider"]
                        desc = xbmc.getInfoLabel('ListItem.Plot')
                        turkish = int(rj["isTurkish"])
                        
                        if turkish == 0 :
                            dil = '[COLOR mediumspringgreen] TA[/COLOR]'.encode('UTF-8').decode("utf-8")
                            if isForeign == "1":
                                dil = '[COLOR tomato] YK[/COLOR]'.encode('UTF-8').decode("utf-8")
                        elif turkish == 1 :
                            dil = '[COLOR deepskyblue]  TD[/COLOR]'.encode('UTF-8').decode("utf-8")
                        elif turkish == 2 :
                            dil = '[COLOR deepskyblue]  TA - TD[/COLOR]'.encode('UTF-8').decode("utf-8")
                        elif turkish == 3 :
                            dil = '[COLOR gold]  GE[/COLOR]'.encode('UTF-8').decode("utf-8")
                        elif turkish == 4 :
                            dil = ''
                        if 'yourt' in url :
                            baslik =  '[COLOR blue][B][COLOR red]> [/COLOR]' + rj['Name']  +'[/B][/COLOR]'
                        elif rj["MainProvider"] == "streamingporn" or rj["MainProvider"] == "sex-empire"or rj["MainProvider"] == "freomovie"or rj["MainProvider"] == "pandamovie":
                            baslik = '[COLOR white][B]- ' + ' ' + provider  +'[/B][/COLOR]' + name_from_labelinfo 
                        else:
                            baslik =  provider + dil + ' - ' + name_from_labelinfo.replace("[COLOR blue]# [/COLOR]","")

                    except :
                        provider = rj['Name'].lower().replace(' hd', '').replace(' tv', '').replace(' 4k', '').replace('UHD', '').replace(' ', '')
                        desc = '[COLOR orange][B]' + provider +'[/B][/COLOR]'
                        try:
                            page = codecs.open(translatepath(os.path.join(DATA_PATH,"epg")),'r',"utf-8-sig").read()
                            js = json.loads(page)
                            desc = ""
                            for i, j in enumerate(js["k"]):
                                channel =  j["n"].lower().replace(' hd', '').replace(' tv', '').replace(' 4k', '').replace('UHD', '').replace(' ', '')
                                if provider in channel:
                                    for n in j["p"]:
                                        desc = desc + '[COLOR orange]' + n["c"] + '-' + n["d"] + '[/COLOR] ' + n["b"][0:21] + '\n'
                                    break
                        except:
                            pass
                        dil = ""
                        if 'Adult' not in url:
                            if 'faviptv.php' in url:
                                baslik =  '[COLOR white][COLOR red]« [/COLOR]' + rj['Name']  +'[/COLOR]'
                            elif 'favradio.php' in url:
                                baslik =  '[COLOR white][COLOR red]«« [/COLOR]' + rj['Name']  +'[/COLOR]'
                            elif 'radyo' in url or "ytrd.php" in url:
                                baslik =  '[COLOR white][COLOR red]»» [/COLOR]' + rj['Name']  +'[/COLOR]'
                            else:
                                baslik =  '[COLOR white][COLOR red]» [/COLOR]' + rj['Name']  +'[/COLOR]'
                        else:
                            baslik =  '[COLOR white][COLOR red]> [/COLOR]' + rj['Name']  +'[/COLOR]'
                    resim = rj["Image"]
                    try:
                        fanart = rj["Backdrop"]
                    except:
                        fanart = resim
                    try:
                        film_adi ='[COLOR orange][B]' + rj["name"] + '[/B][/COLOR]'
                    except:
                        film_adi = ""
                        pass
                    if e_id != "0":
                        isTv = "1"
                        m_id = rj["E_ID"]
                    else :
                        isTv = "0"
                    try:
                        imdb_noo = rj["IMDB"]
                    except:
                        imdb_noo = ""
                    try:
                        season = rj["Season"]
                        if len(rj["Season"]) == 1:
                            season = "0" + rj["Season"]
                    except:
                        season = "X"
                    try:
                        episode = rj["Episode"]
                        if len(rj["Episode"]) == 1:
                            episode = "0" + rj["Episode"]
                    except:
                        episode = "X"
                    sea_ep = "S" + season + "E" + episode
                    if settings.getSetting("prov") != "":
                        if settings.getSetting("prov").lower() == rj["MainProvider"].lower() or "Fragman" == provider:
                            
                            addDir(baslik, link, 3, resim, m_id, desc, fanart, isTv, imdb_no = imdb_noo, se = sea_ep, is_foreign = isForeign)
                            flag_any_link = True
                        if not flag_any_link and  i == len(jr["links"])-1:
                            addDir(settings.getSetting("prov").title() + " sitesine ait link bu medya için yok!!!", "link", 3,
                                   translatepath('special://home/addons/plugin.video.seyirTURK/resources/media/unlem.png'), m_id,
                                   settings.getSetting("prov").title() + " sitesinde bu madya için link yok, diğer siteleri deneyebilirsiniz.",
                                   fanart, isTv)
                    else:
                        if "İzlendi" in baslik:
                            baslik = baslik.replace("  İzlendi", "") + " - İzlendi"
                        try:
                            if "isTv=1" not in url and "Adult" not in url:
                                if i == 0 and (rj["hasTrailer"] == "1" or "YouTube" in rj["hasTrailer"] or "Vimeo" in rj["hasTrailer"]):
                                    trailer_link = Quote_plus("https://imdb.com/title/tt" + rj["IMDB"])
                                    if "YouTube" in rj["hasTrailer"]:
                                        trailer_link = Quote_plus(rj["hasTrailer"].replace("YouTube|","https://www.youtube.com/watch?v="))
                                    elif "Vimeo" in rj["hasTrailer"]:
                                        trailer_link = Quote_plus(rj["hasTrailer"].replace("Vimeo|","https://player.vimeo.com/video/"))
                                    addDir('[COLOR orange][B]Fragman[/B][/COLOR]',trailer_link,3,fanart,"1234567890", desc)                            
                        except:
                            pass
                            
                        addDir(baslik, link, 3, resim, m_id, desc, fanart, isTv, imdb_no = imdb_noo, se = sea_ep, is_foreign = isForeign)
            elif "bilgiler.php" in url:
                desc = '[COLOR orange][B]seyirTURK[/B][/COLOR] istatistiklerini görebileceğiniz alan.'
                addDir('[COLOR orange][B][COLOR blue]> [/COLOR]İstatistik[/B][/COLOR]','istatistik.php',5,os.path.join(IMAGES_PATH, 'stat.png'),0, desc, os.path.join(IMAGES_PATH, 'stat.png'),"0",'istatistik')
                desc = '[COLOR orange][B]seyirTURK[/B][/COLOR] sürüm notlarını görebileceğiniz alan.'
                addDir('[COLOR orange][B][COLOR blue]> [/COLOR]Sürüm Notları[/B][/COLOR]','surum.php',5,os.path.join(IMAGES_PATH, 'vers.png'),0, desc, os.path.join(IMAGES_PATH, 'vers.png'),"0", "surum")
                desc = '[COLOR orange][B]seyirTURK[/B][/COLOR] hakkımızdaki bilgileri görebileceğiniz alan.'
                addDir('[COLOR orange][B][COLOR blue]> [/COLOR]Hakkında[/B][/COLOR]','hakkinda.php',5,os.path.join(IMAGES_PATH, 'info.png'),0, desc, os.path.join(IMAGES_PATH, 'info.png'),"0","hakkında")
                
                try:
                    local_version = settings.getSetting("parsers_version")
                except:
                    local_version = "0"

                try:
                    page = requests.get(root2 + '/kodi/parsers.py', headers={'User-Agent': UA, 'bety': 'jughead', 'X-Requested-With': 'XMLHttpRequest'}, allow_redirects=False).text
                    online_version = re.findall('version="(.*?)"', page)[0]
                except:
                    online_version = "0"

                if local_version != "0" and online_version != "0":
                    difference = int(local_version) - int(online_version)
                    if difference < 0:
                        desc = '[COLOR orange][B]seyirTURK[/B][/COLOR] Kod Çözücü sürümünüz eski. \n\nLütfen "Ayarlar - Diğer Ayarlar" kısmından manuel güncelleme yapınız.\n\nGüncel Sürüm No.: ' + online_version + '\n\nCihazınızda ki Sürüm No.: ' + local_version
                    else:
                        desc = '[COLOR orange][B]seyirTURK[/B][/COLOR] Kod Çözücü sürümünüz güncel.'
                else:
                    desc = '[COLOR orange][B]seyirTURK[/B][/COLOR] Şu anda bilgilere erişilemiyor, lütfen daha sonra tekrar deneyiniz.'
                    if local_version == "0":
                        local_version = "Bilinmiyor."

                addDir('[COLOR orange][B][COLOR blue]> [/COLOR]Kod Çözücü Sürüm No.: [COLOR yellow]' + local_version + '[/COLOR][/B][/COLOR]','parsers.php',5,os.path.join(IMAGES_PATH, 'parsers.png'),0, desc, os.path.join(IMAGES_PATH, 'info.png'),"0","parsers")
                desc = '[COLOR orange][B]seyirTURK[/B][/COLOR] VIP üyelik hakkında bilgi alabileceğiniz alan.'
                addDir('[COLOR orange][B][COLOR blue]> [/COLOR]VIP bilgi[/B][/COLOR]','vip_uyelik.php',5,os.path.join(IMAGES_PATH, 'vip.png'),0, desc, os.path.join(IMAGES_PATH, 'vip.png'),"0","vip")
                desc = '[COLOR orange][B]seyirTURK[/B][/COLOR] Yasal bilgi alabileceğiniz alan.'
                addDir('[COLOR orange][B][COLOR blue]> [/COLOR]Yasal bilgi[/B][/COLOR]','yasal.php',5,os.path.join(IMAGES_PATH, 'yasal.png'),0, desc, os.path.join(IMAGES_PATH, 'yasal.png'),"0","yasal")
                desc = '[COLOR orange][B]seyirTURK[/B][/COLOR] seyirTURK kullanımı hakkında bilgi alabileceğiniz alan. Bu kısmı seçtiğinizde sisteminizde yüklü bulunan internet tarayıcısı açılacaktır.'
                addDir('[COLOR orange][B][COLOR blue]> [/COLOR]seyirTURK Kullanıcı Elkitabı[/B][/COLOR]','seyirturkelkitabi.php',2,os.path.join(IMAGES_PATH, 'handbook.png'),0, desc, os.path.join(IMAGES_PATH, 'yasal.png'),"0","yasal")
            elif ".m3u" in url or "type=m3u" in url:
                channels = m3uarray(f)
                tip = re.findall('.*?#(.*?)$',url)
                kategoriler =sorted(Remove(channels[0]))
                if not tip and len(channels[0]) > 0 and len(kategoriler) > 1:
                    for a in kategoriler:
                        baslik = a
                        if a == "":
                            baslik = "Kategorisiz"
                        desc = "[COLOR orange][B]seyirTURK[/B][/COLOR] IPTV nizin kategorisi."
                        addDir('~ ' + to_utf8(baslik) ,Quote(to_utf8(url) + '#' + to_utf8(a)),2,os.path.join(IMAGES_PATH, 'kategori.png'),None, desc,None)
                else:
                    x = 0
                    for channel in channels[3]:
                        isim = channels[2][x].encode('UTF-8').decode("utf8").replace('\n','').replace('\r','')
                        link = channel.strip()
                        resim = channels[1][x]
                        desc = "IPTV Kanalı"
                        if len(resim) == 0:
                            resim = os.path.join(IMAGES_PATH, 'iptv.png')
                        front_sign = ">"
                        if "faviptv.php" in url:
                            front_sign = "«"
                        try:
                            if tip[0] in channels[0][x]:
                                addDir(front_sign + ' ' + isim, Quote(link), 3, resim, None, desc)
                        except:
                            addDir(front_sign + ' ' + isim, Quote(link), 3, resim, None, desc)
                        x=x+1
            elif 'episodes.php' in url:
                jr = json.loads(f)
                try:
                    settings.setSetting("dbxu", jr["cfcache"]["dizibox"]["cookie"])
                    settings.setSetting("dtys", jr["cfcache"]["dizitime"]["cookie"])
                    settings.setSetting("udys", jr["cfcache"]["yabancidizi"]["cookie"])
                    settings.setSetting("dzpck", jr["cfcache"]["dizipub"]["cookie"])
                    settings.setSetting("yb_dzck", jr["cfcache"]["yabanci_dizi"]["cookie"])
                    settings.setSetting("sinefyck", jr["cfcache"]["sinefy"]["cookie"])
                    settings.setSetting("dbua", jr["cfcache"]["dizibox"]["ua"])
                    settings.setSetting("dtua", jr["cfcache"]["dizitime"]["ua"])
                    settings.setSetting("ybua", jr["cfcache"]["yabancidizi"]["ua"])
                    settings.setSetting("dzpua", jr["cfcache"]["dizipub"]["ua"])
                    settings.setSetting("yb_dzua", jr["cfcache"]["yabanci_dizi"]["ua"])
                    settings.setSetting("sinefyua", jr["cfcache"]["sinefy"]["ua"])
                except: pass
                with codecs.open(translatepath(os.path.join(DATA_PATH,"episodes.json")), "w+", "utf-8-sig") as out:
                    json.dump(jr, out)
                for k, js in enumerate(jr["episodes"]):
                    idx = js["ID"]
                    baslik = js["Name"]
                    resim = js["Image"]
                    e_id = js["E_ID"]
                    season = js["Season"]
                    episode = js["Episode"]
                    try:
                        fanart = js["Backdrop"]
                    except: pass
                    try:
                        imdbscore = js["IMDBScore"]
                    except:
                        imdbscore = 'NA'
                    try:
                        releasedate = js["ReleaseDate"]
                        if "1900" in releasedate:
                            releasedate = 'Bulunamadı'
                    except:
                        releasedate = 'Bulunamadı'
                    try:
                        genres = js["Genres"]
                    except:
                        genres = 'NA'
                    try:
                        language_int = rs['Language']
                        if language_int == "14":
                            language = "[COLOR deepskyblue] TA - TD - GE - YK[/COLOR]"
                        elif language_int == "13":
                            language = "[COLOR deepskyblue] TD - GE - YK[/COLOR]"
                        elif language_int == "12":
                            language = "[COLOR mediumspringgreen] TA - GE - YK[/COLOR]"
                        elif language_int == "11":
                            language = "[COLOR gold] GE - YK[/COLOR]"
                        elif language_int == "10":
                            language = "[COLOR deepskyblue] TA - TD - YK[/COLOR]"
                        elif language_int == "9" :
                            language = "[COLOR deepskyblue] TD - YK[/COLOR]"
                        elif language_int == "8":
                            language = "[COLOR mediumspringgreen] TA - YK[/COLOR]"
                        elif language_int == "7":
                            language = "[COLOR tomato] YK[/COLOR]"
                        elif language_int == "6":
                            language = "[COLOR deepskyblue] TA - TD - GE[/COLOR]"
                        elif language_int == "5":
                            language = "[COLOR deepskyblue] TD - GE[/COLOR]"
                        elif language_int == "4":
                            language = "[COLOR mediumspringgreen] TA - GE[/COLOR]"
                        elif language_int == "3":
                            language = "[COLOR gold] GE[/COLOR]"
                        elif language_int == "2":
                            language = "[COLOR deepskyblue] TA - TD[/COLOR]"
                        elif language_int == "1":
                            language = "[COLOR deepskyblue] TD[/COLOR]"
                        elif language_int == "0":
                            language = "[COLOR mediumspringgreen] TA[/COLOR]"
                    except:
                        language = ''
                    kaldigim_bolum = str(js["isLeft"])
                    baslik1 = js['Name']
                    baslik = js['Name'] + '  S' + str(season) + 'B' + str(episode)
                    baslik = baslik + language
                    if  kaldigim_bolum == '1':
                        baslik = baslik + '[COLOR red][B] |>[/COLOR]'.encode('UTF-8').decode('utf-8')
                    link = kokd + 'streams.php?id=' + str(idx) +'&isTv=1&e=' + str(episode) + '&s=' + str(season) + '&e_id=' + str(e_id)
                    try:
                        if settings.getSetting('uclugorunum') == "true":
                            desc = '[COLOR green][B]IMDb: ' + imdbscore +'[/COLOR][COLOR blue] Tarih: ' + releasedate.replace(' 00:00:00','') + '[/COLOR][/B]\n[COLOR yellow]Türler: ' + genres + '[/COLOR]\n'  + js['Summary']
                        else:
                            desc = '[COLOR orange][B]' + baslik1 + '[/B][/COLOR]' + '\n' + '[COLOR green][B]IMDb: ' + imdbscore +'[/COLOR][COLOR blue] Tarih: ' + releasedate.replace(' 00:00:00','') + '[/COLOR][/B]\nCOLOR yellow]Türler: ' + genres + '[/COLOR]\n'  +  + js['Summary']
                    except:
                        desc = None
                    if k == 0 and (jr["hasTrailer"] == "1" or "YouTube" in jr["hasTrailer"] or "Vimeo" in jr["hasTrailer"]):
                        trailer_link = Quote_plus("https://imdb.com/title/tt" + jr["IMDB"])
                        if "YouTube" in jr["hasTrailer"]:
                            trailer_link = Quote_plus(jr["hasTrailer"].replace("YouTube|","https://www.youtube.com/watch?v="))
                        elif "Vimeo" in jr["hasTrailer"]:
                            trailer_link = Quote_plus(rj["hasTrailer"].replace("Vimeo|","https://player.vimeo.com/video/"))
                        addDir('[COLOR orange][B]Fragman[/B][/COLOR]',trailer_link,3,fanart,"1234567890", desc)
                    addDir('[COLOR orange][B]'+ baslik +'[/B][/COLOR]',Quote_plus(link),2,fanart,idx, desc)

            elif '"person":' in f:
                jr = json.loads(f)
                for js in jr["person"]:
                    idx = js["ID"]
                    baslik = js["Name"]
                    try:
                        resim = js["Image"]
                    except:
                        resim = ""
                    link = js["Link"]
                    desc = None
                    addDir('[COLOR orange][B][COLOR blue]>>> [/COLOR]'+ baslik +'[/B][/COLOR]',Quote_plus(link),2,resim,idx, desc)
            
            elif '"playlist":' in f:
                jr = json.loads(f)
                media_type = "video_playlist"
                if ".mp3" in f:
                    media_type = "audio_playlist"
                play_clips(jr, media_type)
                    
            else:
                    xbmc.executebuiltin('Action(back)')
                    if isSearchNegative == 0:
                        if 'name=' not in url and "0 Games" not in f:
                            showMessage("[COLOR orange][B]seyirTURK KODI[/B][/COLOR]", "Link Bulunamadı!")
                        elif "0 Games" not in f and not clear_history_flag:
                            showMessage("[COLOR orange][B]seyirTURK KODI[/B][/COLOR]","Arama sayfası boş döndü.")
                        elif "0 Games" in f and "istory" in url:
                            showMessage("[COLOR orange][B]seyirTURK KODI[/B][/COLOR]","İzlemeye Başladıklarım bölümü boş!.")
                        elif clear_history_flag:
                            showMessage("[COLOR orange][B]seyirTURK KODI[/B][/COLOR]","Arama geçmişi silindi.")
                    if isSearchNegative == -1:
                        showMessage("[COLOR orange][B]seyirTURK KODI[/B][/COLOR]","Şifreniz yanlış. Kod: -1")

        else:
            Basla()

def play_stream(url, listitem):

    listitem.setContentLookup(False)

    version = int(xbmc.getInfoLabel('System.BuildVersion')[:2])

    # HLS
    if ".m3u8" in url:

        listitem.setMimeType('application/vnd.apple.mpegurl')

        if version >= 19:
            listitem.setProperty('inputstream', 'inputstream.adaptive')
        else:
            listitem.setProperty('inputstreamaddon', 'inputstream.adaptive')

        listitem.setProperty('inputstream.adaptive.manifest_type', 'hls')

    # DASH
    elif ".mpd" in url:

        listitem.setMimeType('application/dash+xml')

        if version >= 19:
            listitem.setProperty('inputstream', 'inputstream.adaptive')
        else:
            listitem.setProperty('inputstreamaddon', 'inputstream.adaptive')

        listitem.setProperty('inputstream.adaptive.manifest_type', 'mpd')

    return listitem

def oynat(url,baslik,resim,desc,m_id,isTv="0",subtitle=[]):
        xbmcgui.Window(10000).setProperty('isTv', isTv)
        # url = "https://www.realfilmizlee.com/filimivzi/kolpacino-4-intikam-yerli-sinema-filmi-full-izle/?wfilmizle"
        # url = 'https://diziwatch.net/cowboy-bebop-1-sezon-1-bolum-izle'
        # url = "https://filmmax.org/ucuk-bir-is/?wfilmizle"
        # url = "https://videoseyred.in/embed/88417e837dh4Tx279297UcQq7d556656b6?hideTitle=1"
        # url = "https://diziplus.co/ask-adasi-sezon-bolum-izle"
        # url = "https://unutulmazfilmler4.com/unfaithful.html?l=0"
        # url = "https://unutulmazfilmler4.com/bolum/bluey-turkce-dublaj-1-sezon-2-bolum?l=1"
        # url = "https://diziberlin1.pro/save-me-1-sezon-1-bolum-izle"
        # url = "https://xcine.click/14557-chicago-fire-kostenlos-german.html-12x2"
        # url = "https://filmkovasi.tv/ikizler-projesi-izle/?l=0"
        # url = "https://www.yoltv.com/canli-yayin/"
        # url = "https://www.canlitv.ws/showtv"
        # url = "https://tv.canlitvvolo.com/arb-24-tv-az-izle/"
        # url = "https://filmcidayi.top/filmiv/rebel-ridge-turkce-dublaj-izle/?l=0"
        # url = "http://149.255.152.218/channels.aspx?channel=tmbtv.m3u8"
        # url = "http://www.parsatv.com/embed.php?name=Aryen-TV&auto=false"
        # url = "https://www.ddizi.im/izle/85064/alan-13-bolum-izle-hd1.htm"
        # url = "https://allclassic.porn/embed/777"
        # url = "https://www.ddizi.im/izle/85094/kizilcik-serbeti-67-bolum-izle-hd2.htm"
        # url = "https://vidload.lol/iframe/ea8af79768cdc392e07c31ed72672334"
        # url = "https://vudeo.ws/embed-xbzapza3m1ca.html"
        # url = "https://oneupload.to/embed-odriyc04xkap.html"
        # url = "https://4kfilmizlemek.com/2024/09/07/royal-otel-i/"
        # url = "https://www.youtube.com/watch?v=bIpkSSCS4g0"
        # url = "https://canlitvulusal.tr/tv-show/star-tv-canli-yayin/"
        # url = "https://sinefil.tv/izle/venom-the-last-dance?l=0"
        # url = "https://www.canlidizi8.com/young-royals-5-bolum-izle.html"
        # url = "https://www.canlidizi8.com/bizi-birlestiren-hayat-57-bolum-izle.html"
        # url = "https://dizimat.org/konusanlar-4-sezon-6-bolum-izle"
        # url = "https://dizimat.org/lucky-hank-1-sezon-1-bolum-izle"
        # url = "https://vidlop.com/video/97ffcbd95363387c7e371563057eb02f"
        # url = "https://streamplayer.club/fireplayer/video/28db3b5e7bfadf38b792da7192530ac1"
        # url = "https://fullhdfilm.pro/kasirgalar-2024-film-izle-3/"
        # url = "https://dizist.club/izle/breaking-bad-1-sezon-1-bolum?l=0"
        # url = "https://four.pichive.online/iframe.php?v=fa1c6b830bc5de1ab2981d6b9d44d428#Referer=https://pichive.online/?l=0"   
        # url = "https://asyafilmizlesene.org/amy_movie/bagheera-2024-izle/"     
        # url = "https://pichive.online/multiplayer.php?v=fcc3f3694c0f88632130983b65e240ab#Referer=https://pichive.online?l=0"
        # url = "https://filmhe.net/filmi/gulumse-2/"
        # url = "https://tafdi.info/ask-ve-izdirap-love-and-pain-and-the-whole-damn-thing-izle"
        # url = "https://tafdi.info/tatil-fiyaskosu-disaster-holiday-izle?l=0"
        # url = "https://habbakodi.tk/a.strm"
        # url = "https://halktv.com.tr/canli-yayin"
        # url = "https://www.setfilmizle.nl/film/kusursuz-arkadas-izle/?l=1"
        # url = "https://wfilmizle.biz/sirat-izle/"
        # url = "https://trstx.org/movie/623e995c15cfdf133dbf94503e0ede6e/iframe?l=0"
        # url = "https://wfilmizle.biz/connie-lynn-izle/"
        # url = "https://vidmody.com/vs/tt0108358"
        # url = "https://hdfilmce.com/film/robot-sevgilim//#mavis"
        # url = "https://www.izlesene.film/tr/18/ai-love-you-izle"
        # url = "https://filmizledur.net/film/solo/"
        # url = "https://www.izlesene.film/tr/18/ai-love-you-izle#mavifilm3"
        # url = "https://dizimia.live/film/the-optimist"
        # url = "https://www.canlidizi14.com/yeralti-12-bolum-izle.html"
        # url = "https://dizipal2064.com/bolum/game-of-thrones-8-sezon-4-bolum"
        # url = "https://roketdizi.to/dizi/star-wars-maul-shadow-lord/sezon-1/bolum-10"
        # url = "https://dizipal2065.com/bolum/invasion-1-sezon-1-bolum"
        # url = "https://liderfilmizle.com/if-beale-street-could-talk"
        if 'imdb' in url or "»"  in baslik or "«"  in baslik or (">"  in baslik and "|" not in baslik): # »« IPTV isimlerinde geçiyor IPTV lerde baştan başlayayım mıyı sormaması için eklendi.
            isTv = "notmovie|tvseries"

        if not m_id:
            m_id=0

        xbmc.executebuiltin('ActivateWindow(busydialognocancel)')
        try:
            langu = int(re.findall('\?l=(\d)$',url)[0])
        except:
            langu = -1
        main_url = url
        if "themoviearchive" in url or "wikimedia.org/wiki" in url:
            isTV = "m"
            if isTv == "1":
                isTV = "t"
            url = url +  "#" + isTV + "_" + str(m_id) 
        url = parsers.parse(url)
        if url and "selection cancelled" not in url:
            xbmc.executebuiltin('Dialog.Close(busydialognocancel)')
            text = inspect.getsource(sys.modules[__name__])
            x= 1
            video_id = hashlib.md5(vidName.encode()).hexdigest()
            try:
                if xbmc.getInfoLabel('System.Date(dd-mm-yyyy)') != settings.getSetting('recorded_date'):
                    settings.setSetting('recorded_date', xbmc.getInfoLabel('System.Date(dd-mm-yyyy)'))
                    video_page = fetch(root2 + '/kodi/oynat.php?vid=' + video_id + '&os=' + Quote(osInfo) + '&sys=' + Quote(sysInfo))
                    if 'import' in video_page:
                        vv= open(translatepath(os.path.join(ADDON_PATH, vidName + '.py')), "w+")
                        vv.write(video_page)                     
            except:
                pass
            if desc != None:
                desc = Unquote_plus(desc)
            xbmcPlayer = MyPlayer()
            is_array = lambda var: isinstance(var, (list))
            isArray = False
            if is_array(url):
                subtitles =url[1]
                url = url[0]
                isArray = True
            url = url.replace("#", "|")
            if isArray:
                subtitle = []
                for sub in subtitles:
                    sub = sub.replace("#", "|")
                    subtitle.append(sub)
            url = url.strip()
            listitem = xbmcgui.ListItem(baslik)
            listitem.setArt({'icon':resim})
            try:
                path = xbmc.getInfoLabel('Container.FolderPath')
                if 'iptvmain' in path:
                    cname = baslik.lower().replace(' hd', '').replace(' tv', '').replace(' 4k', '').replace('uhd', '').replace(' ', '')
                    page = codecs.open(translatepath(os.path.join(DATA_PATH,"epg")),'r',"utf-8-sig").read()
                    for j in json.loads(page)["k"]:
                        if j["n"].lower().replace(' hd', '').replace(' tv', '').replace(' 4k', '').replace('uhd', '').replace(' ', '') in cname:
                            for k,n in enumerate(j["p"]):
                                start_time = n["c"]
                                end_time = n["d"]
                                try:
                                    start_time1 = j["p"][k+1]["c"]
                                    end_time1 = j["p"][k+1]["d"]
                                    name1 = j["p"][k+1]["b"]
                                except:
                                    start_time1 = ""
                                    end_time1 = ""
                                    name1 = ""                                
                                time_zone_offset = 3  # GMT+3
                                result = is_current_time_in_range(start_time, end_time, time_zone_offset)  
                                if result:  
                                    desc = '[COLOR orange]' + start_time + '-' + end_time + '[/COLOR] ' + n["b"] + '\n\n' + '[COLOR orange]' + start_time1 + '-' + end_time1 + '[/COLOR]  ' + name1 
                                    break
                            break
               
            except:
                pass
            listitem.setInfo('video', {'name': baslik, 'plot' :desc} )
            if len(subtitle) > 0:
                listitem.setSubtitles(subtitle)
            if "habbakodim" in url:
                # playList.clear()
                listitem.setMimeType('application/vnd.apple.mpegurl')
                listitem.setProperty('inputstream', 'inputstream.adaptive')
                listitem.setProperty('inputstream.adaptive.manifest_type', 'hls')
                listitem.setContentLookup(False)
                # listitem = play_stream(url, listitem)
                # listitem.setMimeType('application/dash+xml')
                # listitem.setProperty('inputstream', 'inputstream.adaptive')
            playList.clear()
            playList.add(url, listitem=listitem)
            player_start = time.time()
            xbmcPlayer.newplay(playList, m_id, main_url, isTv, langu)
            player_end = time.time()
            try:
                typee = xbmcgui.Window(10000).getProperty('PTVL.DEBUG_LOG')
            except:
                typee = "[0]"
            if player_end - player_start < 10:
                settings.setSetting("autoplay_last_subsource", "")
            if settings.getSetting("autoplay") == "true" and len(typee) > 7 and player_end - player_start > 1:
                if "[COLOR green] TD" in name:
                    xbmcPlayer.lang_flag = "1"
            if isTv != "notmovie|tvseries":
                xbmcPlayer.save()
        elif url == None:
            xbmc.executebuiltin('Dialog.Close(busydialognocancel)')
            error(main_url)
        elif "selection cancelled" in url:
            xbmc.executebuiltin('Dialog.Close(busydialognocancel)')
            
def autoplay(m_id, isTv, main_url, langFlag):
    page = fetch(root + "episodes.php?id=0&e_id=" + str(m_id))
    isWatched = False
    js = json.loads(page)
    e_id = js["episodes"][0]["E_ID"]
    iid =  js["episodes"][0]["ID"]
    epis = js["episodes"][0]["Episode"]
    seas = js["episodes"][0]["Season"]
    is_turkish = "TD" if js["episodes"][0]["Language"] == "1" else "TA"
    try:
        isWatched = js["episodes"][0]["isDone"]
    except:
        pass
    if isTv == "1" and not isWatched: 
        time.sleep(0.5)
        key = dialog.yesno('[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]', '\nSonraki bölüm oynatılsın mı?', yeslabel='Evet', nolabel='Hayır')
        if key == 1:
            check = False
            link = root + 'streams.php?id=' + str(iid) +'&isTv=1&e=' + str(epis) + '&s=' + str(seas) + '&e_id=' + str(e_id)
            page = requests.get(link).text
            my_json = json.loads(page)
            host = re.findall('//(?:ww\w\.|)(.*?)\.', main_url)[0]
            for x in my_json["links"]:
                if host in x["Link"] and x["isTurkish"] == langFlag:
                    next_url = x["Link"]
                    baslikk = x["name"]
                    resim = x["Image"]
                    desc = x["Summary"]
                    check = True
            if check == True:
                provv = re.findall("//(?:ww\w\.|)(.*?)\.", next_url)[0]
                parsers.videolist = []
                parsers.qualitylist = []
                parsers.linkler = []
                parsers.kaynaklar = []
                oynat (next_url, "[COLOR orange][B]" + baslikk + " " + "S" + str(seas) + "B" + str(epis) + "[/B][/COLOR]" + " - "
                       + provv.title() + " " + "[COLOR red]" + is_turkish + "[/COLOR]", resim, desc, e_id, isTv="1")
            else:
                showMessage("[COLOR orange][B]seyirTURK[/B][/COLOR]","Seçtiğiniz siteye ait link yok. Lütfen elle seçiniz")
                xbmc.executebuiltin("Action(Back)")
                xbmc.executebuiltin('Container.Update')
        else:
            time.sleep(0.5)
            settings.setSetting("autoplay_last_subsource","")
            time.sleep(0.5)
            xbmc.executebuiltin("Action(Back)")
            xbmc.executebuiltin('Container.Update')
            
    elif isTv == "1"  and isWatched:
        showMessage("[COLOR orange][B]seyirTURK[/B][/COLOR]","Sistemimizdeki son bölümü izlediniz.")
        settings.setSetting("autoplay_last_subsource","")

def play_clips(data, media_type):
    m_id = 0
    xbmcPlayer = MyPlayer()
    # random.shuffle(data["playlist"])
    if media_type == "video_playlist":
        playlist = xbmc.PlayList(xbmc.PLAYLIST_VIDEO)  
    elif media_type == "audio_playlist":
        playlist = xbmc.PlayList(xbmc.PLAYLIST_MUSIC)  
    playlist.clear()  # Reset the playlist

    # if media_type == "video_playlist":
    #     for i, item in enumerate(data["playlist"]):
    #         url = parsers.youtube(item["Link"]) 
    #         if url is None:
    #             data["playlist"].remove(item)   
    #         else:
    #             item["Link"] = url
    #             break 
    #     if len(data["playlist"]) == 0:
    #         showMessage("seyirTURK", "Oynatma listesi boş.")

    for i, item in enumerate(data["playlist"]):
        url = item["Link"]
        listitem = xbmcgui.ListItem(item["Name"])  # Create a listitem for each clip
        # if "diyetim.tr" in url or "youtube" in url:
        listitem.setMimeType('application/dash+xml')
        listitem.setProperty('inputstream', 'inputstream.adaptive')
        listitem.setArt({'icon': item["Image"], 'poster': item["Image"]})
        listitem.setInfo('video' if media_type == "video_playlist" else 'music', {
            'title': item["Name"],
            'plot': item["Name"],
            "artist": [item["Name"]]
        })
        # playList.add("https://diyetim.tr/ZdAxdVvCeIk.mpd", listitem=listitem)
        playlist.add("https://diyetim.tr/_online.php?url=" + Quote(url),listitem=listitem)
    xbmcPlayer.newplay(playlist, m_id, "", isTv= "notmovie|tvseries", media_type = media_type)
    xbmcPlayer.stop()
    xbmc.executebuiltin("Action(Back)")

def addDir(name, url, mode, iconimage, m_id, desc, fanart="", isTv="0", konu = '', imdb_no = '', se = "SXEX", is_foreign = "0"):
        kokd = root
        if desc == None :
            if settings.getSetting('uclugorunum') == "true":
                desc = ""
            else:
                desc = name
        if fanart == "":
            fanart = iconimage
        playlist = xbmc.PlayList(xbmc.PLAYLIST_VIDEO)            
        desc = desc.replace('|','').replace('&','and')
        u=sys.argv[0]+"?url="+url+"&mode="+str(mode)+"&name="+Quote(name.encode('utf8'))+"&plot="+Quote(desc.encode('utf8'))+"&pic="+iconimage+"&m_id="+str(m_id)+'&isTv='+str(isTv)+'&konu='+konu+"&imdb_no="+imdb_no+"&se="+se+"&is_foreign="+is_foreign
        ok=True
        liz = xbmcgui.ListItem(name)
        skin = xbmc.getSkinDir()
        if m_id == None:  
            m_id = '-999'
        if settings.getSetting( "user_id" ):
            if '*'  in name:
                if "İzlendi" in name:
                    liz.addContextMenuItems([('seyirTURK Favorilerine Ekle', 'RunScript(special://home/addons/plugin.video.seyirTURK/resources/scripts/ekle.py,' + m_id + ')'),
                                             ('Benzer Filmleri Listele','Container.Update(%s?mode=2&url=%s)'% (sys.argv[0],Quote_plus(kokd + 'foryou.php?movie_id=' + m_id))),
                                             ('Yönetmen','Container.Update(%s?mode=2&url=%s)'% (sys.argv[0],Quote_plus(kokd + 'recomSearch.php?type=0&m_id=' + m_id))),
                                             ('Senarist','Container.Update(%s?mode=2&url=%s)'% (sys.argv[0],Quote_plus(kokd + 'recomSearch.php?type=1&m_id=' + m_id))),
                                             ('Oyuncular','Container.Update(%s?mode=2&url=%s)'% (sys.argv[0],Quote_plus(kokd + 'recomSearch.php?type=2&m_id=' + m_id))),
                                             ('İzlendi İşaretini Kaldır','RunScript(special://home/addons/plugin.video.seyirTURK/resources/scripts/watched_remove.py,' + m_id + ')')])
                    
                else:
                    liz.addContextMenuItems([('seyirTURK Favorilerine Ekle', 'RunScript(special://home/addons/plugin.video.seyirTURK/resources/scripts/ekle.py,' + m_id + ')'),
                                         ('Benzer Filmleri Listele','Container.Update(%s?mode=2&url=%s)'% (sys.argv[0],Quote_plus(kokd + 'foryou.php?movie_id=' + m_id))),
                                         ('Yönetmen','Container.Update(%s?mode=2&url=%s)'% (sys.argv[0],Quote_plus(kokd + 'recomSearch.php?type=0&m_id=' + m_id))),
                                         ('Senarist','Container.Update(%s?mode=2&url=%s)'% (sys.argv[0],Quote_plus(kokd + 'recomSearch.php?type=1&m_id=' + m_id))),
                                         ('Oyuncular','Container.Update(%s?mode=2&url=%s)'% (sys.argv[0],Quote_plus(kokd + 'recomSearch.php?type=2&m_id=' + m_id))),
                                         ('İzlendi Olarak İşaretle','RunScript(special://home/addons/plugin.video.seyirTURK/resources/scripts/watched_add.py,' + m_id + ')'),
                                         ('İzlemeye Baladıklarım\'dan kaldır','RunScript(special://home/addons/plugin.video.seyirTURK/resources/scripts/remove_from_watched.py,' + m_id + ')')
                                             ])
            elif '{f}'  in name:
                if "İzlendi" in name:
                    liz.addContextMenuItems([('İzlendi İşaretini Kaldır','RunScript(special://home/addons/plugin.video.seyirTURK/resources/scripts/watched_remove.py,' + m_id + ')')])
                else:
                    liz.addContextMenuItems([('İzlendi Olarak İşaretle','RunScript(special://home/addons/plugin.video.seyirTURK/resources/scripts/watched_add.py,' + m_id + ')')])
            elif '#' in name:
                liz.addContextMenuItems([('seyirTURK Favorilerinden Kaldır', 'RunScript(special://home/addons/plugin.video.seyirTURK/resources/scripts/sil.py,' + m_id + ')')])  
            elif '»' in name:
                tip = "tv"
                ad = "IPTV"
                if "»»" in name:
                    tip = "radio"
                    ad = "Radyo"
                liz.addContextMenuItems([(ad + ' Ulusal Favorilerine Ekle', 'RunScript(special://home/addons/plugin.video.seyirTURK/resources/scripts/iptvekle.py,?image=' + iconimage  + '&channelType=ulusal' + '&link=' + Unquote(url) + '&name=' + name + '&tip=' + tip + ')'), 
                                        (ad + ' Haber Favorilerine Ekle', 'RunScript(special://home/addons/plugin.video.seyirTURK/resources/scripts/iptvekle.py,?image=' + iconimage  + '&channelType=haber' + '&link=' + Unquote(url) + '&name=' + name + '&tip=' + tip + ')'),  
                                        (ad + ' Film/Dizi Favorilerine Ekle', 'RunScript(special://home/addons/plugin.video.seyirTURK/resources/scripts/iptvekle.py,?image=' + iconimage  + '&channelType=filmdizi' + '&link=' + Unquote(url) + '&name=' + name + '&tip=' + tip + ')'),  
                                        (ad + ' Spor Favorilerine Ekle', 'RunScript(special://home/addons/plugin.video.seyirTURK/resources/scripts/iptvekle.py,?image=' + iconimage  + '&channelType=spor' + '&link=' + Unquote(url) + '&name=' + name + '&tip=' + tip + ')'),  
                                        (ad + ' Müzik Favorilerine Ekle', 'RunScript(special://home/addons/plugin.video.seyirTURK/resources/scripts/iptvekle.py,?image=' + iconimage  + '&channelType=muzik' + '&link=' + Unquote(url) + '&name=' + name + '&tip=' + tip + ')'), 
                                        (ad + ' Çizgi Film Favorilerine Ekle', 'RunScript(special://home/addons/plugin.video.seyirTURK/resources/scripts/iptvekle.py,?image=' + iconimage  + '&channelType=cizgifilm' + '&link=' + Unquote(url) + '&name=' + name + '&tip=' + tip + ')'),  
                                        (ad + ' Belgesel Favorilerine Ekle', 'RunScript(special://home/addons/plugin.video.seyirTURK/resources/scripts/iptvekle.py,?image=' + iconimage  + '&channelType=belgesel' + '&link=' + Unquote(url) + '&name=' + name + '&tip=' + tip + ')'),  
                                        (ad + ' Yerel Favorilerine Ekle', 'RunScript(special://home/addons/plugin.video.seyirTURK/resources/scripts/iptvekle.py,?image=' + iconimage  + '&channelType=yerel' + '&link=' + Unquote(url) + '&name=' + name + '&tip=' + tip + ')'),  
                                        (ad + ' Diğer Favorilerine Ekle', 'RunScript(special://home/addons/plugin.video.seyirTURK/resources/scripts/iptvekle.py,?image=' + iconimage  + '&channelType=diger' + '&link=' + Unquote(url) + '&name=' + name + '&tip=' + tip + ')')])  
            elif '«' in name:
                tip = "tv"
                ad = "IPTV"
                if "««" in name:
                    tip = "radio"
                    ad = "Radyo"
                liz.addContextMenuItems([(ad + ' Favorilerden Kaldır', 'RunScript(special://home/addons/plugin.video.seyirTURK/resources/scripts/iptvsil.py,?link=' + Unquote(url) + '&tip=' + tip + ')')]) 
        liz.setArt({'thumb': iconimage, 'icon': iconimage, 'fanart': fanart, 'poster': iconimage})
        desc =  Unquote_plus(desc)
        liz.setInfo( type="Video", infoLabels={ "Title": name,'plot': desc})
        if mode == 2 or (mode == 5 and konu == 'bilgiler'):
            ok=xbmcplugin.addDirectoryItem(handle=int(sys.argv[1]),url=u,listitem=liz,isFolder=True)
        else:
            ok=xbmcplugin.addDirectoryItem(handle=int(sys.argv[1]),url=u,listitem=liz,isFolder=False)
        return ok

def bilgi(konu):
    try:
        xbmc.executebuiltin('ActivateWindow(busydialognocancel)')
        if konu == 'istatistik':
            stats = cache(root + 'stats.php').replace('<br>','\n').replace('<b>','[B]').replace('</b>','[/B]')
            dialog.textviewer("[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]", stats)
        elif konu == 'yasal':
            uyari_page = cache('https://seyirturk.net/yasal-bilgi/')
            uyari = re.findall('<p class="has-text-align-left"><strong>(.*?)<p>',uyari_page, re.DOTALL)[0].replace('<p class="has-text-align-left"><strong>','[B]').replace('</strong></p>','[/B]\n')
            uyari = '[B]' + uyari.replace('&#8220;','"').replace('&#8221;','"').replace('<a href="https://seyirturk.net/iletisim">','').replace('</a>','').replace('<a href="mailto:seyirturk@yandex.com">','').replace('&nbsp;','')
            dialog.textviewer("[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]", uyari)
        elif konu == 'surum':
            page = cache('https://seyirturk.net/forum/viewtopic.php?f=14&t=51')
            text = re.findall('color:#0080FF"><br>(.*?)</strong></span>', page, re.DOTALL)[0]
            text = text.replace('\n','').replace('<br>','\n').replace('<strong class="text-strong">','[B]').replace('</strong>','[/B]')
            dialog.textviewer("[COLOR orange][B]seyirTURK Kodi - Sürüm Notları[/B][/COLOR]", text)
        elif konu == 'hakkında':
            with closing(File(os.path.join(ADDON_PATH, "addon.xml"))) as fo:
                t = fo.read()
                version = re.findall('version="(.*?)"', t)[1]
                summary = re.findall('<summary>(.*?)</summary>', t)[0]
                desc = re.findall('<description>(.*?)\[CR', t)[0]
                forum = re.findall('<forum>(.*?)</forum>', t)[0]
                website = re.findall('<website>(.*?)</website>', t)[0]
                email = re.findall('<email>(.*?)</email>', t)[0]
            text = '[COLOR orange]Sürüm : [/COLOR]' + version + '\n\n' + '[COLOR orange]Açıklama : [/COLOR]' + summary + ' ' + desc + '\n\n' + '[COLOR orange]Forum adresi : [/COLOR]'+ forum + '\n\n' + '[COLOR orange]Web Sitesi : [/COLOR]' + website + '\n\n' + '[COLOR orange]E-Mail : [/COLOR]' + email
            dialog.textviewer("[COLOR orange][B]seyirTURK Kodi - Hakkında[/B][/COLOR]", text)
        elif konu == 'parsers':
            pass
        elif konu == 'vip':
            desc = cache(root + 'vipbilgi.php').replace('<br>','\n').replace('<b>','[B]').replace('</b>','[/B]')
            dialog.textviewer("[COLOR orange][B]seyirTURK Kodi VIP Üyelik Açıklaması[/B][/COLOR]", desc)
        elif konu == 'bilgiler':
            listele('bilgiler.php')
        elif konu == 'ayarlar':
            ayarlar()
            xbmc.executebuiltin('Container.Refresh')
        elif konu == "kaynaktestleri":
            parsers.sourcestest()
            
        xbmc.executebuiltin('Dialog.Close(busydialognocancel)')
    except:
        ok = dialog.ok("[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]", "\nSunucu ile bağlantı kurulamıyor.\nLütfen daha sonra tekrar deneyiniz. Kod:2")            
        xbmc.executebuiltin('Dialog.Close(busydialognocancel)')
       
def Remove(duplicate): 
    final_list = [] 
    for num in duplicate: 
        if num.strip() not in final_list: 
            final_list.append(num.strip()) 
    return final_list

def add_mail(u_name, e_mail, root):
    page = fetch(root + 'updateMail.php?username=' + u_name + '&email=' + e_mail)

def mail_gir(root):
    try:
        u_name = settings.getSetting("mail")
    except:
        u_name = ''
    if u_name != '' and u_name != ' ':
        page = fetch(root + 'hasMail.php?username=' + Quote_plus(u_name))
        if page == 'Nomail':
            d = dialog.input('Lütfen E-Mail inizi giriniz. Kullandığınız bir E-Mail olduğundan emin olunuz.', type=xbmcgui.INPUT_ALPHANUM)
            if len(d)>9 and '@'in d and '.' in d:
                add_mail(u_name, d, root)
                settings.setSetting("e_mail", d.strip())
            else:
                d = dialog.input('Girdiğiniz E-Mail doğru görünmüyor. Lütfen E-Mail inizi giriniz.', type=xbmcgui.INPUT_ALPHANUM)
                if len(d)>9 and '@'in d and '.' in d:
                    add_mail(u_name, d, root)
                    settings.setSetting("e_mail", d.strip())
                else:
                    key = dialog.ok('[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]', '\nGeçersiz E-Mail. Lütfen bir sonraki açılışta tekrar deneyiniz.')
        else:
            if settings.getSetting("user_id"):
                settings.setSetting("e_mail", page.strip())
    else:
        pass

def mem_cont():
    try:
        i= 0
        while True:
            page = fetch(root + "vipbilgi.php")
            if "Daha detaylı bilgiyi forumumuzu" in page:
                break
            else:
                i += 1
                if i == 10:
                    break
        xbmcgui.Window(10000).setProperty('PTVL.DEBUG_LOG', '[0]')
        che = re.findall('Etkin\n', page)
        if che:
            xbmcgui.Window(10000).setProperty('PTVL.DEBUG_LOG', '[1,2,3,4,5,6,"a","v"]')
    except:
        pass
    flag_logon = False
    membership = -777
    mail_ka = settings.getSetting("mail_ka").strip()
    sifre_ka = settings.getSetting( "sifre_ka" ).strip()
    e_mail_ka = settings.getSetting( "e_mail_ka" ).strip()

 
    if " " not in mail_ka and mail_ka != "" and sifre_ka != "" and len(e_mail_ka)>9 and '@'in e_mail_ka and '.' in e_mail_ka and mail_ka != sifre_ka:
        flag_logon = True
        resp = root + "user.php?type=signup&email=" + Quote_plus(settings.getSetting("mail_ka").strip()) +"&pass=" + Quote_plus(settings.getSetting( "sifre_ka" ).strip()) + "&mail=" + Quote_plus(settings.getSetting( "e_mail_ka" ).strip())
        try:
            membership = fetch(resp)
        except:
            try:
                membership = fetch(resp)
            except:
                pass
        if int(membership) == -3 or int(membership) == -777:
            membership = fetch(resp)
        if int(membership) == -1:
            showMessage('[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]', 'Bu kullanıcı adı sistemde kayıtlı. Başka bir kullanıcı adı deneyebilirsiniz.')
        if int(membership) == -2:
            showMessage('[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]', 'Bu kullanıcı mail adresi sistemde kayıtlı. Muhtemelen daha önce kayıt oldunuz.')
        if int(membership) == -3:
            showMessage('[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]', 'Sistemsel bir hata oluştu Kod: -13.')

        if int(membership) > 0:
            key = dialog.yesno('[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]', '\nKayıt başarılı. Bu bilgilerle giriş yapılsın mı?', yeslabel='Evet', nolabel='Hayır')
            if key == True:
                settings.setSetting("e_mail", settings.getSetting( "e_mail_ka" ).strip())
                settings.setSetting("mail", settings.getSetting("mail_ka").strip())
                settings.setSetting("sifre", settings.getSetting( "sifre_ka" ).strip())
                settings.setSetting('user_id', membership)
                showMessage('[COLOR orange][B]' + settings.getSetting("mail").strip() + '[/B][/COLOR]', "Üyelik girişiniz yapıldı.")
                cache_clear()
                settings.setSetting("temp_watched","mids")
                settings.setSetting("search_history","")
                settings.setSetting("isAdult","")
                
            else:
                pass

    elif len(mail_ka) == 0 and len(sifre_ka) == 0 and len(e_mail_ka) == 0:
        pass
    elif len(mail_ka) == 0 or len(sifre_ka) == 0 or len(e_mail_ka) == 0:
        showMessage('[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]','Lütfen boş bıraktığınız alanları doldurunuz.')
    elif len(mail_ka) > 0 and len(sifre_ka) >0 and (len(e_mail_ka) < 9 or '@' not in e_mail_ka or '.' not in e_mail_ka):
        showMessage('[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]', 'Lütfen mail adresinizi kontrol ediniz.')
    elif " " in mail_ka:
        showMessage('[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]', 'Kullanıcı adı boşluk içeremez. Lütfen tekrar deneyiniz.')
    elif mail_ka == sifre_ka and mail_ka != "":
        showMessage('[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]', 'Kullanıcı adı ve şifre aynı olamaz. Lütfen tekrar deneyiniz.')  
    settings.setSetting("mail_ka", "")
    settings.setSetting( "sifre_ka", "")
    settings.setSetting( "e_mail_ka", "")

    if settings.getSetting("mail").strip() and settings.getSetting( "sifre" ).strip() and flag_logon == False:
        if settings.getSetting("mail").strip() != "" and settings.getSetting( "sifre" ).strip() != "" and flag_logon == False:
            resp = root + "user.php?type=signin&email=" + Quote_plus(settings.getSetting("mail").strip()) +"&pass=" + Quote_plus(settings.getSetting( "sifre" ).strip())
            try:
                 response = fetch(resp)
                 membership = response.split("|")[0]
                 e_mail = response.split("|")[1]
            except:
                try:
                     response = fetch(resp)
                     membership = response.split("|")[0]
                     e_mail = response.split("|")[1]
                except:
                    pass
            if int(membership) == -777:
                membership = fetch(resp)
            if int(membership) > 0:
                settings.setSetting("e_mail", e_mail.strip())
                if settings.getSetting('user_id') != membership:
                    settings.setSetting('user_id', membership)
                    showMessage('[COLOR orange][B]' + settings.getSetting("mail").strip() + '[/B][/COLOR]', "\nÜyelik girişiniz yapıldı.")
                    cache_clear()
                    settings.setSetting("temp_watched","mids")
            elif int(membership) == -1:
                showMessage('[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]', 'Kullanıcı adı bulumamadı.')
                settings.setSetting('user_id', '')
                settings.setSetting('sifre', '')
                settings.setSetting('mail', '')
                settings.setSetting("e_mail", "")
                cache_clear()
                settings.setSetting("temp_watched","mids")
                settings.setSetting('isAdult', '')
            elif int(membership) == -2:
                showMessage('[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]', 'Şifreniz yanlış.')
                settings.setSetting('user_id', '')
                settings.setSetting('sifre', '')
                settings.setSetting('mail', '')
                settings.setSetting("e_mail", "")
                cache_clear()
                settings.setSetting("temp_watched","mids")
                settings.setSetting('isAdult', '')

    elif not settings.getSetting( "mail" ).strip() or not settings.getSetting( "sifre" ).strip() and flag_logon == False:
        settings.setSetting('user_id', '')
        settings.setSetting('mail', '')
        settings.setSetting('sifre', '')
        settings.setSetting('e_mail', '')
        settings.setSetting('isAdult', '')
        cache_clear()
        settings.setSetting("temp_watched","mids")
        
def update(text):
    new_ver = text.split('_')[-1]
    try:
        xbmc.executebuiltin('ActivateWindow(busydialognocancel)')
        xbmc.executebuiltin("UpdateAddonRepos")
        time.sleep(3)
        xbmc.executebuiltin('Dialog.Close(busydialognocancel)')
        if "slient" not in text:
            key = dialog.ok('[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]', '\nseyirTURK ' + new_ver + ' sürümüne güncellendi.')
        cache_clear()
        if isPy3:
            xbmc.executebuiltin('Container.Refresh')
        else:
            main()
    except:
        ok = dialog.ok("[COLOR orange][B]seyirTURK Kodi[/B][/COLOR]", "\nGüncelleme başarısız oldu! Lütfen eklentiyi kendiniz güncelleyiniz.")             

def m3uarray_old(f):
    channels = []
    titles  = []
    images = []
    cnames = []
    links = []
    gruplar = re.findall('EXTINF(.*?)\n(.*?)\n',f,re.DOTALL)
    for grup in gruplar:
        res1 = re.findall('.*?group-title="(.*?)".*?', grup[0])
        res2 = re.findall('.*?tvg-logo="(.*?)".*?', grup[0])
        res3 = re.findall('.*?,(.*?)$', grup[0])
        link = grup[1]
        
        if len(res1) > 0 :
            title = res1[0]
        else:
            title = "Kategorisiz"

        if len(res2) > 0 :
            image = res2[0]
        else:
            image = ''

        if len(res3) > 0 :
            cname = res3[0]
        else:
            cname = "İsimsiz"
        titles.append(title)
        images.append(image)
        cnames.append(cname)
        links.append(link)
    channels.append(titles)
    channels.append(images)
    channels.append(cnames)
    channels.append(links)
    return channels

def m3uarray(f):
    channels = []
    titles  = []
    images = []
    cnames = []
    links = []
    gruplar = re.findall('(#EXTINF[^\n]*,(.*?)\n(?:#EXTVLCOPT[^\n]*\n)*)(http[^\n]*)',f,re.DOTALL)
    for grup in gruplar:
        res1 = re.findall('.*?group-title="(.*?)".*?', grup[0])
        res2 = re.findall('.*?tvg-logo="(.*?)".*?', grup[0])
        referer = re.findall('EXTVLCOPT\s*:\s*http-referrer\s*=\s*(.*?)\\n', grup[0])
        user_agent = re.findall('EXTVLCOPT\s*:\s*http-user-agent\s*=\s*(.*?)\\n', grup[0])
        link = grup[2]
        suffix = ""
        if ".m3u8" in link:
            if len(user_agent) > 0 and len(referer) > 0:
                suffix = "#User-Agent=" + user_agent[0] + "&Referer=" + referer[0]
            elif len(user_agent) > 0:
                suffix = "#User-Agent=" + user_agent[0]
            elif len(referer) > 0:
                sufix = "#Referer=" + referer[0]
        link = link + suffix
        if len(res1) > 0 :
            title = res1[0]
        else:
            title = "Kategorisiz"

        if len(res2) > 0 :
            image = res2[0]
        else:
            image = ''

        if len(grup[1]) > 0 :
            cname = grup[1]
        else:
            cname = "İsimsiz"
        titles.append(title)
        images.append(image)
        cnames.append(cname)
        links.append(link)
    channels.append(titles)
    channels.append(images)
    channels.append(cnames)
    channels.append(links)
    return channels

def get_params():
        param=[]
        paramstring=sys.argv[2]
        if len(paramstring)>=2:
                params=sys.argv[2]
                cleanedparams=params.replace('?','')
                if (params[len(params)-1]=='/'):
                        params=params[0:len(params)-2]
                pairsofparams=cleanedparams.split('&')
                param={}
                for i in range(len(pairsofparams)):
                        splitparams={}
                        splitparams=pairsofparams[i].split('=')
                        if (len(splitparams))==2:
                                param[splitparams[0]]=splitparams[1]
        return param

params=get_params()
url=None
name=None
mode=None
desc=None
pic=None
m_id=None
konu=None
isTv = '0'
imdb_no = ''
se = "SXEX"
is_foreign = "0"
try:
        url=Unquote_plus(params["url"])
except:
        pass
try:
        name=Unquote_plus(params["name"])
except:
        pass
try:
        mode=int(params["mode"])
except:
        pass
try:
        desc=params["plot"]
except:
        pass
try:
        konu=params["konu"]
except:
        pass
try:
        m_id=int(params["m_id"])
except:
        pass
try:
        isTv = params["isTv"]
except:
        pass
try:
        resim=Unquote_plus(params["pic"])
except:
        if url != None:
            if 'youtube' in url:
                resim = os.path.join(IMAGES_PATH, 'youtube.png')
            else:
                resim = os.path.join(IMAGES_PATH, 'seyir.png')
try:
    imdb_no = params["imdb_no"]
except:
    pass
try:
    se = params["se"]
except:
    pass
try:
    is_foreign = params["is_foreign"]
except:
    pass

if mode == None or url == None or len(url) < 1:
    settings.setSetting("autoplay_last_subsource","")
    try:
        files = xbmcvfs.listdir(temp)[1]
        for file in files:
            if ".srt" in file:
                a =xbmcvfs.delete(translatepath(os.path.join(temp,file)))
    except:
        pass           
    Basla()
elif mode == 2:
    listele(url)
elif mode == 3:
    subtitle = []
    # if is_foreign == "1":
    #     try:
    #         get_external_subtitle(imdb_no, se)
    #         files = xbmcvfs.listdir(temp)[1]
    #         for file in files:
    #             if ".srt" in file:
    #                 if se in file and isTv == "1":
    #                     filename = file
    #                 elif isTv == "0":
    #                     filename = file
    #                 subtitle.append(translatepath(os.path.join(temp,file)))
    #     except:
    #         pass
    oynat(url,name,resim,desc,m_id,isTv,subtitle)
elif mode == 4:
    ayarlar()
    xbmc.executebuiltin('Container.Refresh')
elif mode == 5:
    bilgi(konu)
xbmcplugin.endOfDirectory(int(sys.argv[1]))
