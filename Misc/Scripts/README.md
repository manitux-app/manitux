# Manitux Desktop Publish Scripts

Bu klasördeki standalone publish scriptleri release asset adını uygulama sürümünden üretir.

## Sürüm Nereden Geliyor?

Scriptler sürümü `Manitux.Desktop/Manitux.Desktop.csproj` içindeki `Version` property değerinden okur:

```bash
dotnet msbuild "$PROJECT" -getProperty:Version -nologo
```

İlgili proje dosyasında güncellenecek alan:

```xml
<Version>0.1.0</Version>
<FileVersion>$(Version)</FileVersion>
<InformationalVersion>$(Version)</InformationalVersion>
```

Güncelleme yayınlamak için asıl yükseltilecek değer `Version` alanıdır. `FileVersion` ve `InformationalVersion` bu değeri takip eder.

## Release Asset Adı

Scriptler varsayılan olarak şu klasörü ve zip dosyasını üretir:

```text
builds/Manitux_<runtime>_v<version>/
builds/Manitux_<runtime>_v<version>.zip
```

Örnek:

```text
builds/Manitux_linux-x64_v0.2.0/
builds/Manitux_linux-x64_v0.2.0.zip
```

Updatum tarafındaki asset eşleşmesi de bu ada göre yapılır:

```text
Manitux_<runtime>_v*.zip
```

## Güncelleme Yayınlama Akışı

1. `Manitux.Desktop/Manitux.Desktop.csproj` içindeki `Version` değerini yükselt.
2. İstenen runtime için publish scriptini çalıştır.
3. GitHub Releases tarafında aynı sürümle tag oluştur.
4. Scriptin ürettiği zip dosyasını release asset olarak yükle.

Örnek:

```text
Version: 0.2.0
Git tag: v0.2.0
Asset: Manitux_linux-x64_v0.2.0.zip
```

## Script Kullanımı

Shell scriptler:

```bash
bash Misc/Scripts/publish-linux-x64-standalone.sh
bash Misc/Scripts/publish-osx-x64-standalone.sh
bash Misc/Scripts/publish-osx-arm64-standalone.sh
```

Opsiyonel olarak çıktı klasörü ve zip yolu verilebilir:

```bash
bash Misc/Scripts/publish-linux-x64-standalone.sh [output-dir] [zip-path]
```

PowerShell scriptler:

```powershell
./Misc/Scripts/publish-win-x64-standalone.ps1
./Misc/Scripts/publish-linux-x64-standalone.ps1
./Misc/Scripts/publish-osx-x64-standalone.ps1
./Misc/Scripts/publish-osx-arm64-standalone.ps1
```

Opsiyonel parametreler:

```powershell
./Misc/Scripts/publish-win-x64-standalone.ps1 -OutputDir <output-dir> -ZipPath <zip-path>
```
