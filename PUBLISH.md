# BatteryNotifier — Publishing & Distribution Guide

## Overview

BatteryNotifier is published as self-contained executables for Windows and macOS. Releases are distributed via GitHub Releases with SHA-256 checksums and Velopack installers. An in-app update checker notifies users when new versions are available.

> **Note:** Linux builds are currently disabled in CI. They can be re-enabled in `.github/workflows/build-and-release.yml`.

---

## Quick Start — Local Build

```bash
# Windows x64
dotnet publish BatteryNotifier.Avalonia/BatteryNotifier.Avalonia.csproj \
  -c Release -r win-x64

# Windows ARM64 (Surface Pro X, Snapdragon laptops)
dotnet publish BatteryNotifier.Avalonia/BatteryNotifier.Avalonia.csproj \
  -c Release -r win-arm64

# macOS Apple Silicon (M1/M2/M3/M4)
dotnet publish BatteryNotifier.Avalonia/BatteryNotifier.Avalonia.csproj \
  -c Release -r osx-arm64

# macOS Intel
dotnet publish BatteryNotifier.Avalonia/BatteryNotifier.Avalonia.csproj \
  -c Release -r osx-x64

# Linux x64
dotnet publish BatteryNotifier.Avalonia/BatteryNotifier.Avalonia.csproj \
  -c Release -r linux-x64

# Linux ARM64 (Raspberry Pi, ARM servers)
dotnet publish BatteryNotifier.Avalonia/BatteryNotifier.Avalonia.csproj \
  -c Release -r linux-arm64
```

The `.csproj` already sets `SelfContained=true`, `PublishSingleFile=true`, `EnableCompressionInSingleFile=true`, and `IncludeNativeLibrariesForSelfExtract=true` — no extra `-p:` flags needed for basic builds.

Output: `BatteryNotifier.Avalonia/bin/Release/net10.0/<rid>/publish/`

### Optional: Trimming

Add `-p:PublishTrimmed=true -p:TrimMode=partial` to reduce binary size. `partial` trims only assemblies that opt in, which is safer for Avalonia's reflection usage.

---

## CI/CD — GitHub Actions

The workflow at `.github/workflows/build-and-release.yml` handles everything:

### On every push/PR:
1. Restores, builds, and tests on all active targets (win-x64, win-arm64, osx-arm64)
2. Publishes self-contained single-file executables
3. Uploads build artifacts

### On version tags (`v*`):
All of the above, plus:
4. Signs Windows executable with signtool
5. Signs + notarizes macOS binary with codesign/notarytool
6. Generates SHA-256 checksums
7. Creates a draft GitHub Release with all artifacts

### Triggering a release:

```bash
# 1. Update version in these files:
#    - BatteryNotifier.Core/Constants.cs (ApplicationVersion)
#    - BatteryNotifier.Avalonia/BatteryNotifier.Avalonia.csproj (Version)
#    - BatteryNotifier.Avalonia/Info.plist (CFBundleVersion + CFBundleShortVersionString)

# 2. Commit and tag:
git add -A
git commit -m "release: v3.3.0"
git tag v3.3.0
git push origin master --tags
```

The workflow creates a **draft** release — review the release notes, then publish manually on GitHub.

---

## Code Signing

### Windows

Requires a code signing certificate (EV or standard).

**GitHub Secrets required:**
| Secret | Description |
|--------|-------------|
| `WINDOWS_CERTIFICATE` | Base64-encoded `.pfx` certificate |
| `WINDOWS_CERTIFICATE_PASSWORD` | PFX password |

**Encode your certificate:**
```bash
base64 -i certificate.pfx | pbcopy  # macOS
certutil -encode certificate.pfx encoded.txt  # Windows
```

**Local signing:**
```bash
signtool sign /f certificate.pfx /p "password" \
  /tr http://timestamp.digicert.com /td sha256 /fd sha256 \
  BatteryNotifier.exe
```

### macOS

Requires an Apple Developer ID certificate and an Apple Developer account for notarization.

**GitHub Secrets required:**
| Secret | Description |
|--------|-------------|
| `MACOS_CERTIFICATE` | Base64-encoded `.p12` Developer ID Application certificate |
| `MACOS_CERTIFICATE_PASSWORD` | P12 password |
| `MACOS_KEYCHAIN_PASSWORD` | Temporary keychain password (any random string) |
| `MACOS_SIGNING_IDENTITY` | Certificate CN, e.g. `"Developer ID Application: Your Name (TEAMID)"` |
| `APPLE_ID` | Apple ID email |
| `APPLE_TEAM_ID` | Apple Developer Team ID |
| `APPLE_APP_PASSWORD` | App-specific password (appleid.apple.com → Security → App-Specific Passwords) |

**Local signing:**
```bash
# Sign
codesign --force --options runtime --timestamp \
  --entitlements BatteryNotifier.Avalonia/Entitlements.plist \
  --sign "Developer ID Application: Your Name (TEAMID)" \
  publish/BatteryNotifier

# Notarize
ditto -c -k --keepParent publish/BatteryNotifier /tmp/notarize.zip
xcrun notarytool submit /tmp/notarize.zip \
  --apple-id "your@email.com" \
  --team-id "TEAMID" \
  --password "app-specific-password" \
  --wait

xcrun stapler staple publish/BatteryNotifier
```

**Entitlements** (`Entitlements.plist`) grant the .NET runtime the JIT and unsigned memory permissions it needs. Without these, the signed binary will crash on Apple Silicon.

### Linux

Linux binaries are not typically code-signed, but you can GPG-sign the release tarball:

```bash
gpg --armor --detach-sign BatteryNotifier-linux-x64.tar.gz
```

Users verify with:
```bash
gpg --verify BatteryNotifier-linux-x64.tar.gz.asc
```

---

## Auto-Update

The app checks GitHub Releases API every 6 hours for new versions via `UpdateService`. When an update is found:

1. A native toast notification appears: "Update Available"
2. The user can click "Check for Updates..." in the tray menu
3. The browser opens to the GitHub release page for manual download

**How it works:**
- `UpdateService` compares `Constants.ApplicationVersion` against the `tag_name` of the latest GitHub release
- Uses `System.Version` comparison (semver-compatible)
- First check happens 2 minutes after startup (avoids slowing launch)
- Rate-limited to the GitHub API's anonymous rate limit (60 req/hour)

**Future enhancement:** Replace browser-based download with Velopack for fully automatic in-place updates. Add `Velopack` NuGet package and call `UpdateManager.CheckForUpdatesAsync()` / `DownloadUpdatesAsync()` / `ApplyUpdatesAndRestart()`.

---

## Tamper Protection & Integrity

### Build-time
- **Single-file bundling**: All managed DLLs are embedded in one executable — harder to replace individual assemblies
- **Embedded PDB**: Stack traces work without shipping separate `.pdb` files (`DebugType=embedded`)
- **Compression**: Embedded assemblies are compressed, adding a layer of obfuscation

### Runtime
- **Settings encryption**: AES-256-GCM with per-machine key (see security docs in CLAUDE.md)
- **Crash marker signing**: HMAC-SHA256 prevents injection of fake crash reports
- **SHA-256 checksums**: Published alongside every release for verification

### Code signing
- **Windows Authenticode**: Prevents "Unknown publisher" warnings and ensures binary integrity
- **macOS Gatekeeper**: Signed + notarized binaries pass Gatekeeper without user intervention
- **Timestamp**: All signatures include RFC 3161 timestamps — valid even after certificate expiry

### What code signing does NOT protect against
- A determined attacker with admin access can still replace the binary
- Code signing proves *publisher identity*, not that the code is free of vulnerabilities
- For high-security environments, consider additional measures like application whitelisting

---

## Release Checklist

1. **Update version** in 3 files:
   - `BatteryNotifier.Core/Constants.cs` → `ApplicationVersion`
   - `BatteryNotifier.Avalonia/BatteryNotifier.Avalonia.csproj` → `<Version>`
   - `BatteryNotifier.Avalonia/Info.plist` → `CFBundleVersion` + `CFBundleShortVersionString`

2. **Run tests locally**:
   ```bash
   dotnet test
   ```

3. **Test publish locally** (at least your current platform):
   ```bash
   dotnet publish BatteryNotifier.Avalonia/BatteryNotifier.Avalonia.csproj -c Release -r osx-arm64
   ./BatteryNotifier.Avalonia/bin/Release/net10.0/osx-arm64/publish/BatteryNotifier
   ```

4. **Commit, tag, push**:
   ```bash
   git commit -am "release: v3.3.0"
   git tag v3.3.0
   git push origin master --tags
   ```

5. **Wait for CI** — builds all active targets, signs (if secrets configured), creates draft release

6. **Review draft release** on GitHub — edit release notes if needed, then publish

7. **Verify checksums** match artifacts:
   ```bash
   sha256sum -c checksums-sha256.txt
   ```

---

## Platform-Specific Notes

### Windows
- Users may see SmartScreen warnings until the signing certificate builds reputation
- EV (Extended Validation) certificates bypass SmartScreen immediately
- Standard certificates need ~1000 downloads to build trust

### macOS
- Unsigned builds require: System Settings → Privacy & Security → "Open Anyway"
- Notarized builds open without any warnings
- `LSUIElement=true` in Info.plist hides the Dock icon (tray-only app)

### Linux
- No installer needed — extract tarball, run binary
- For desktop integration, copy the `.desktop` file to `~/.local/share/applications/`
- May need `chmod +x BatteryNotifier` after extraction

---

## Secrets Summary

| Secret | Platform | Required For |
|--------|----------|-------------|
| `WINDOWS_CERTIFICATE` | Windows | Code signing |
| `WINDOWS_CERTIFICATE_PASSWORD` | Windows | Code signing |
| `MACOS_CERTIFICATE` | macOS | Code signing |
| `MACOS_CERTIFICATE_PASSWORD` | macOS | Code signing |
| `MACOS_KEYCHAIN_PASSWORD` | macOS | CI keychain |
| `MACOS_SIGNING_IDENTITY` | macOS | Code signing |
| `APPLE_ID` | macOS | Notarization |
| `APPLE_TEAM_ID` | macOS | Notarization |
| `APPLE_APP_PASSWORD` | macOS | Notarization |

All secrets are optional — builds work without them, just unsigned.
