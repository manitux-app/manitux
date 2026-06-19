# Manitux macOS Packaging

`package-macos-app.sh` builds the Manitux macOS `.app` bundle, the first-install DMG, and the Updatum update ZIP.

## Outputs

Default `osx-arm64` output:

```text
builds/
  Manitux_osx-arm64_v<version>/
    install.sh
    Manitux.app/
  Manitux_osx-arm64_v<version>.zip
  Manitux_osx-arm64_v<version>.dmg
```

`<version>` is read from the MSBuild `Version` property in `Manitux.Desktop/Manitux.Desktop.csproj`.

## Usage

Apple Silicon:

```bash
bash Misc/Scripts/Macos/package-macos-app.sh osx-arm64
```

Intel Mac:

```bash
bash Misc/Scripts/Macos/package-macos-app.sh osx-x64
```

Optional explicit output paths:

```bash
bash Misc/Scripts/Macos/package-macos-app.sh osx-arm64 \
  builds/Manitux_osx-arm64_v0.0.1 \
  builds/Manitux_osx-arm64_v0.0.1.zip \
  builds/Manitux_osx-arm64_v0.0.1.dmg
```

The older wrapper scripts delegate to this flow:

```bash
bash Misc/Scripts/publish-osx-arm64-standalone.sh
bash Misc/Scripts/publish-osx-x64-standalone.sh
```

## DMG vs ZIP

The DMG is for first-time installation. The user is expected to run `install.sh` from the DMG once.

The ZIP is for Updatum. GitHub Releases must include the `Manitux_osx-<arch>_v<version>.zip` asset. The application filters update assets by `.zip`, so the DMG is not used by automatic updates.

For Updatum, the ZIP root contains `Manitux.app` directly:

```text
Manitux.app/
  Contents/
    MacOS/
    Frameworks/
    Resources/
```

For manual installation, the DMG root contains:

```text
install.sh
Manitux.app
```

## App Bundle Layout

The script transforms the publish output into a macOS bundle:

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
      helper executables
    Resources/
      Manitux.icns
```

`Contents/MacOS/data` is for bundled default files. Persistent JSON, plugin, and settings files are not written into the bundle; they are written to the platform-specific user data directory.

## Native Files

macOS `.dylib` files are placed in `Contents/Frameworks`, which is the most appropriate bundle location for native libraries.

Helper executables are placed in `Contents/Helpers`.

The runtime code supports these macOS locations:

- `libtlsclient.dylib`: `Contents/Frameworks`
- `libmpv.dylib`: `Contents/Frameworks`
- `tlsclientapi`: `Contents/Helpers`

Linux and Windows behavior is unchanged; those platforms keep using the existing `libs` layout.

## Icon

macOS icon source:

```text
Manitux/Assets/icons/Manitux.icns
```

The script copies it to `Manitux.app/Contents/Resources/Manitux.icns` and writes `CFBundleIconFile` into `Info.plist`.

## RPATH Patching

`patch-macho-rpaths.py` is still required. It patches `/nix/store/...` LC_RPATH entries in `Contents/Frameworks/*.dylib` so the libraries can work inside the app bundle.

On macOS, if `install_name_tool` is available, the script also tries to add this rpath to the main executable:

```text
@executable_path/../Frameworks
```

## Install Script

The DMG `install.sh`:

- Copies `Manitux.app` to `/Applications`.
- Uses `sudo` when needed.
- Clears extended attributes, including `com.apple.quarantine` and `com.apple.provenance`.
- Fixes permissions for the main executable, helper files, and dylib files.
- Locally ad-hoc codesigns the bundle when `codesign` is available.

These steps are intended to reduce quarantine and permission issues after the user runs `install.sh` once. They are not a replacement for official Apple notarization.

## DMG Creation

On macOS, if `hdiutil` is available, the script creates a compressed UDZO DMG.

On Linux, if `hdiutil` is not available but `genisoimage` is available, the script creates an HFS/ISO hybrid DMG:

```bash
genisoimage -R -J -joliet-long -D -hfs -mac-name ...
```

This image can be mounted on macOS. The `genisoimage` output is not an Apple UDZO image, but it is usable as a macOS-readable disk image.

## Updatum Update Notes

The first install can use the DMG, but in-app updates require the ZIP asset in GitHub Releases.

Expected asset names:

```text
Manitux_osx-arm64_v<version>.zip
Manitux_osx-x64_v<version>.zip
```

The Updatum integration:

- Uses `AssetExtensionFilter = ".zip"`.
- Expects the `Manitux_<runtime>_v<version>.zip` naming contract.
- Requires `Manitux.app` at the ZIP root for macOS app bundle updates.
- Uses `InstallUpdateCodesignMacOSApp = true` to try local codesigning after update.

Upload both assets to a release:

- `.dmg`: manual first install.
- `.zip`: automatic Updatum updates.

## Verification Commands

Check script syntax:

```bash
bash -n Misc/Scripts/Macos/package-macos-app.sh
```

Check the ZIP root:

```bash
unzip -l builds/Manitux_osx-arm64_v0.0.1.zip | head
```

Check DMG contents on Linux:

```bash
isoinfo -J -f -i builds/Manitux_osx-arm64_v0.0.1.dmg | head
```
