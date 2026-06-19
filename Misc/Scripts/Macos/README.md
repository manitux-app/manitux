# Manitux macOS Paketleme

Bu klasördeki `package-macos-app.sh` scripti Manitux için macOS `.app`, ilk kurulum DMG'si ve Updatum güncelleme ZIP'i üretir.

## Ne Üretir?

Varsayılan `osx-arm64` çıktısı:

```text
builds/
  Manitux_osx-arm64_v<version>/
    install.sh
    Manitux.app/
  Manitux_osx-arm64_v<version>.zip
  Manitux_osx-arm64_v<version>.dmg
```

`<version>` değeri `Manitux.Desktop/Manitux.Desktop.csproj` içindeki MSBuild `Version` alanından okunur.

## Kullanım

Apple Silicon:

```bash
bash Misc/Scripts/Macos/package-macos-app.sh osx-arm64
```

Intel Mac:

```bash
bash Misc/Scripts/Macos/package-macos-app.sh osx-x64
```

İsteğe bağlı olarak çıktı yollarını da verebilirsin:

```bash
bash Misc/Scripts/Macos/package-macos-app.sh osx-arm64 \
  builds/Manitux_osx-arm64_v0.0.1 \
  builds/Manitux_osx-arm64_v0.0.1.zip \
  builds/Manitux_osx-arm64_v0.0.1.dmg
```

Eski wrapper scriptleri de aynı akışa yönlendirilir:

```bash
bash Misc/Scripts/publish-osx-arm64-standalone.sh
bash Misc/Scripts/publish-osx-x64-standalone.sh
```

## DMG ve ZIP Ayrımı

DMG ilk kurulum içindir. Kullanıcının DMG içindeki `install.sh` dosyasını bir kez çalıştırması beklenir.

ZIP Updatum içindir. GitHub Release'e mutlaka `Manitux_osx-<arch>_v<version>.zip` asset'i yüklenmelidir. Updatum kodu `.zip` asset filtreler ve macOS güncellemesini bu dosya üzerinden uygular.

Bu nedenle ZIP'in kökünde doğrudan `Manitux.app` bulunur:

```text
Manitux.app/
  Contents/
    MacOS/
    Frameworks/
    Resources/
```

DMG'nin kökünde ise kurulum için şunlar bulunur:

```text
install.sh
Manitux.app
```

## App Bundle Yapısı

Script publish çıktısını macOS bundle düzenine çevirir:

```text
Manitux.app/
  Contents/
    Info.plist
    PkgInfo
    MacOS/
      Manitux.Desktop
      data/
    Frameworks/
      *.dylib
    Helpers/
      helper executable dosyaları
    Resources/
      Manitux.icns
```

`Contents/MacOS/data` klasörü paketle gelen varsayılan dosyalar içindir. Uygulamanın yazdığı kalıcı JSON, plugin ve ayar dosyaları bundle içine yazılmaz; platforma uygun kullanıcı veri dizinine yazılır.

## Native Dosyalar

macOS için `.dylib` dosyaları `Contents/Frameworks` altına konur. Bu, macOS bundle yapısı için en sorunsuz yerdir.

Helper executable dosyaları `Contents/Helpers` altına konur.

Kod tarafında macOS için bu yollar desteklenir:

- `libtlsclient.dylib`: `Contents/Frameworks`
- `libmpv.dylib`: `Contents/Frameworks`
- `tlsclientapi`: `Contents/Helpers`

Linux ve Windows davranışı değiştirilmez; bu platformlarda `libs` yapısı korunur.

## İkon

macOS icon kaynağı:

```text
Manitux/Assets/icons/Manitux.icns
```

Script bu dosyayı `Manitux.app/Contents/Resources/Manitux.icns` olarak kopyalar ve `Info.plist` içine `CFBundleIconFile` olarak ekler.

## RPATH Düzeltmesi

`patch-macho-rpaths.py` hâlâ gereklidir. Özellikle Nix ortamından gelen `/nix/store/...` LC_RPATH kayıtlarını bundle içinde çalışacak hâle getirmek için `Contents/Frameworks/*.dylib` üzerinde çalışır.

Script macOS üzerinde `install_name_tool` bulursa ana executable'a şu rpath'i de eklemeye çalışır:

```text
@executable_path/../Frameworks
```

## Kurulum Scripti

DMG içindeki `install.sh`:

- `Manitux.app` paketini `/Applications` altına kopyalar.
- Gerekirse `sudo` kullanır.
- `com.apple.quarantine` ve `com.apple.provenance` dahil extended attribute'ları temizler.
- Ana executable, helper dosyaları ve dylib dosyaları için izinleri düzeltir.
- `codesign` varsa bundle'ı ad-hoc olarak yeniden imzalar.

Bu adımlar kullanıcı `install.sh` dosyasını bir kez çalıştırdıktan sonra karantina ve izin sorunlarını azaltmak içindir. Resmî Apple notarization yerine geçmez.

## DMG Üretimi

macOS üzerinde `hdiutil` varsa script sıkıştırılmış UDZO DMG üretir.

Linux üzerinde `hdiutil` yoksa ve `genisoimage` varsa script HFS/ISO hybrid DMG üretir:

```bash
genisoimage -R -J -joliet-long -D -hfs -mac-name ...
```

Bu imaj macOS tarafında mount edilebilir. `genisoimage` ile üretilen dosya Apple UDZO formatında değildir, fakat macOS'un açabileceği disk image olarak kullanılır.

## Updatum Güncelleme Notları

İlk kurulum DMG ile yapılabilir, ancak uygulama içi güncelleme için GitHub Release'te `.zip` asset bulunmalıdır.

Beklenen asset adı:

```text
Manitux_osx-arm64_v<version>.zip
Manitux_osx-x64_v<version>.zip
```

Updatum entegrasyonu:

- `AssetExtensionFilter = ".zip"` kullanır.
- `Manitux_<runtime>_v<version>.zip` adını bekler.
- macOS güncellemesi için zip kökünde `Manitux.app` bulunmalıdır.
- `InstallUpdateCodesignMacOSApp = true` ile güncelleme sonrası lokal codesign yapmaya çalışır.

Release'e hem `.dmg` hem `.zip` yüklenmelidir:

- `.dmg`: kullanıcının elle ilk kurulum yapması için.
- `.zip`: Updatum otomatik güncellemesi için.

## Kontrol Komutları

Script syntax kontrolü:

```bash
bash -n Misc/Scripts/Macos/package-macos-app.sh
```

ZIP kökünü kontrol et:

```bash
unzip -l builds/Manitux_osx-arm64_v0.0.1.zip | head
```

DMG içeriğini Linux üzerinde kontrol et:

```bash
isoinfo -J -f -i builds/Manitux_osx-arm64_v0.0.1.dmg | head
```
