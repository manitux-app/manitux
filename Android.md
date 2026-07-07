# Android Notlari

Bu dosya, Manitux Android hedefi icin yapilan degisikliklerin ve dogrulama notlarinin kalici kaydidir.

## Kural

- Android icin yapilan her UI, davranis, build, paketleme veya cihaz testi degisikligi bu dosyaya tarihli olarak yazilacak.
- Kayitlarda degisen dosyalar, sebep, dogrulama komutu ve varsa cihazdaki gozlem kisaca belirtilecek.
- Surum artirma veya paketleme yalnizca acikca istendiginde yapilacak.

## 2026-07-07

- Android'de 1920x1080 fiziksel ekranda arayuzun dusuk cozunurluk gibi buyuyup poster ve butonlari sismirmesi icin kompakt Android olcek katmani eklendi.
- `Manitux/Views/MainView.axaml.cs` icinde Android'e ozel layout resource override, daha dusuk telefon breakpoint'i ve daha dar desktop rail genisligi tanimlandi.
- `Manitux.Ui/Themes/Layout.axaml` icinde favori, detay ve benzer icerik poster boyutlari resource anahtarlarina tasindi.
- `Manitux.Ui/Themes/Controls.axaml` icinde `android-performance` sinifi Android'de buton min boyutlarini, padding'i, nav ikonlarini ve poster focus buyumesini daha kompakt hale getirecek sekilde genisletildi.
- `Manitux/Pages/Favorites.axaml` ve `Manitux/Pages/MediaInfo.axaml` icindeki sabit poster boyutlari resource anahtarlarina baglandi.
- Dogrulama: `dotnet build Manitux.slnx` basarili. Mevcut nullable/XML yorum uyarilari disinda hata yok.

### Android bosluk ve chrome sikilastirma

- Posterler kuculdukten sonra aralarin fazla acik kalmasi nedeniyle `Manitux/Pages/PageItemShelf.axaml` icinde Android'e ozel shelf spacing, ListBox padding, item margin ve poster margin degerleri daraltildi.
- `Manitux/Pages/PageItems.axaml` ve `Manitux/Pages/PageItems.axaml.cs` icinde Android sinifi eklenerek kategori satirlari arasindaki dikey bosluk azaltildi.
- Sol rail'in fazla yer kaplamasi nedeniyle `Manitux/Views/MainView.axaml` ve `Manitux/Views/MainView.axaml.cs` icinde Android rail genisligi, dis margin, column spacing, rail padding, logo boyutu ve nav stack araliklari kucultuldu.
- Top bar'in fazla yer kaplamasi nedeniyle `Manitux/Pages/PluginTopBar.axaml` ve `Manitux/Pages/PluginTopBar.axaml.cs` icinde Android top bar padding/margin, kolon araligi, plugin secici genisligi ve action buton boyutlari kucultuldu.
- Dogrulama: `dotnet build Manitux.slnx --no-restore` basarili. Sandbox icinde Avalonia BuildServices `buildtasks.log` izin hatasi verdi; izinli calistirmada build gecti. Mevcut nullable/event uyarilari disinda hata yok.

### Poster ve raf bosluklarini minimuma indirme

- `Manitux/Pages/PageItemShelf.axaml` icinde Android shelf spacing `1`, title font/line height `14/16`, ListBox padding `0,1,0,3`, poster item margin `1`, poster button margin `1` yapildi.
- Poster alt baslik bandi padding'i Android'de `5` yapilarak poster ici dikey kayip azaltildi.
- `Manitux/Pages/PageItems.axaml` icinde Android kategori listesi padding'i `0,0,2,2`, raflar arasi margin `0,0,0,2` yapildi.
- Dogrulama: `dotnet build Manitux.slnx --no-restore` basarili. Mevcut nullable/event uyarilari disinda hata yok.
