[English](README_en.md)

# PluginTemplate

`PluginTemplate`, Manitux eklentisi geliştirmek isteyenler için başlangıç projesidir. Örnek kategoriler, liste elemanları, medya detayları, bölümler ve video kaynakları içerir.

## Derleme

Depo kökünden şu komutu çalıştırın:

```bash
dotnet build PluginTemplate/PluginTemplate.csproj -c Release
```

Derleme çıktısı şu konumda oluşur:

```text
PluginTemplate/bin/Release/net10.0/PluginTemplate.dll
```

## Video kaynaklarını çözümleme

Manitux.Core içinde hazır extractor sınıfları bulunur. Bunlar `Manitux.Core/Extractors/` altında tanımlıdır ve Dailymotion, YouTube, Okru, MailRu, Voe, VidMoly, StreamWish, Filemoon, MixDrop, StreamTape, Supervideo, Uqload, DoodStream ve benzeri servisleri destekler.

Bir `VideoSourceModel.Url` bu extractor sınıflarından birinin `MainUrl` veya `SupportedDomains` değeriyle eşleşirse, plugin içinde doğrudan `ExtractAsync` metodunu kullanabilirsiniz. `PluginBase.ExtractAsync`, URL için uygun extractor'ı `ExtractorManager.GetExtractorByUrl` ile bulur; eşleşme varsa ilgili extractor'ın `ExtractAsync(videoSource, referer)` metodunu çalıştırır, eşleşme yoksa mevcut `VideoSourceModel` değerini geri döndürür.

Örnek:

```csharp
public override Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
{
    return ExtractAsync(videoSource, Config.MainUrl);
}
```

Extractor sonucu `Url`, `Referer`, `Headers` ve `Subtitles` alanlarını güncelleyebilir. Bu yüzden desteklenen servislerde embed veya paylaşım URL'sini elle parçalamak yerine önce hazır extractor akışını kullanın; yalnızca eşleşmeyen kaynaklar için özel çözümleme kodu yazın.

## Yayınlama

Bu repo artık yayın çıktılarını `main` dalındaki `builds/` dizininde tutar. Template eklentiyi yayınlamak için derlenmiş DLL'i `builds/` altına kopyalayın ve `builds/plugins.json` içine aynı DLL için bir kayıt ekleyin.

Örnek DLL kaydı:

```json
{
  "url": "https://raw.githubusercontent.com/YOUR_GITHUB_USER/YOUR_PLUGIN_REPOSITORY/main/builds/PluginTemplate.dll",
  "status": 1,
  "version": 1,
  "apiVersion": 1,
  "authors": ["Your Name"],
  "repositoryUrl": "https://github.com/YOUR_GITHUB_USER/YOUR_PLUGIN_REPOSITORY",
  "plugins": [
    {
      "name": "Plugin Template",
      "internalName": "plugin.template",
      "description": "Başlangıç Manitux eklentisi.",
      "language": "tr",
      "iconUrl": "https://www.google.com/s2/favicons?domain=example.com&sz=64",
      "isAdult": false,
      "tvTypes": ["Movie", "TvSeries"]
    }
  ]
}
```

`PluginTemplate.cs` içindeki örnek verileri gerçek kategori, listeleme, medya bilgisi ve video kaynak çözümleme mantığıyla değiştirin. Kod tarafındaki `PluginManifest.Id` değeri, `builds/plugins.json` içindeki `internalName` değeriyle aynı olmalıdır.
