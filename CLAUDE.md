# BatteryNotifier — CLAUDE.md

Cross-platform battery monitoring app built with **Avalonia UI** and **.NET 10**. Notifies the user when the battery is full or low, runs in the system tray, and supports themes, custom sounds, and startup behaviour.

---

## Solution Structure

```
BatteryNotifier/
├── BatteryNotifier.sln
├── BatteryNotifier.Core/          # Platform-agnostic logic (net10.0)
│   ├── Constants.cs
│   ├── Logger/
│   │   ├── BatteryNotifierLogger.cs
│   │   └── BatteryNotifierLoggerConfig.cs
│   ├── Managers/
│   │   ├── BuiltInSounds.cs         # Generates WAV notification tones at runtime
│   │   ├── CustomSoundsLibrary.cs   # User-imported custom sound library
│   │   ├── NotificationManager.cs   # Emits notifications + plays sounds
│   │   └── SoundManager.cs          # Cross-platform audio playback
│   ├── Providers/
│   │   └── BatteryInfoProvider.cs   # WMI Win32_Battery query
│   ├── Services/
│   │   ├── AppSettings.cs           # Encrypted settings singleton (DPAPI / AES-GCM)
│   │   ├── BatteryMonitorService.cs # 1s polling + WMI/Darwin events
│   │   ├── NotificationService.cs   # Priority queue, escalating backoff, throttling
│   │   ├── NotificationTemplates.cs # Level-aware + escalation-aware message templates
│   │   ├── PowerUsageService.cs     # Top CPU consumers → battery drain detection
│   │   ├── ProcessTips.cs           # Known app tips + system process exclusion list
│   │   ├── SettingsEncryption.cs    # AES-GCM encrypt/decrypt for settings at rest
│   │   ├── StartupManager.cs        # Cross-platform launch at startup
│   │   └── SystemStateDetector.cs   # DND / fullscreen detection (all platforms)
│   ├── Store/
│   │   └── BatteryManagerStore.cs   # Shared battery state (singleton)
│   └── Utils/
│       ├── Debouncer.cs
│       └── ProcessRunner.cs         # Shared subprocess runner (ArgumentList, timeout, bounded output)
│
├── BatteryNotifier.Avalonia/      # Avalonia UI app (net10.0)
│   ├── Assets/
│   │   ├── Images, icon
│   │   └── Sounds/                # Bundled "Editor's Choice" sound files
│   ├── Services/
│   │   ├── BundledSounds.cs         # Editor's Choice sounds from Assets/Sounds/
│   │   ├── NotificationDisplayService.cs   # Full notification pipeline (DND, display, sound)
│   │   ├── NotificationPlatformService.cs  # Native OS toast (osascript/powershell/notify-send)
│   │   ├── PlatformHelper.cs        # Cross-platform URL opening + text sanitization
│   │   └── TrayIconService.cs       # System tray icon + menu + window management
│   ├── ViewModels/
│   │   ├── ViewModelBase.cs
│   │   ├── MainWindowViewModel.cs   # Hosts CurrentView, battery data, navigation, DND monitor
│   │   ├── HealthDashboardViewModel.cs  # Battery health + drainers for bottom sheet
│   │   ├── ProcessDisplayItem.cs    # Display model: battery impact, watts, tips per process
│   │   ├── CpuBarConverters.cs      # IValueConverters for drain bar width + color
│   │   ├── SettingsViewModel.cs     # All settings with auto-save + SoundOption model
│   │   ├── SoundPickerViewModel.cs  # Sound picker with built-in, bundled, and custom groups
│   │   └── BatteryNotificationSectionViewModel.cs  # Reusable notification config section
│   ├── Views/
│   │   ├── MainWindow.axaml/.cs
│   │   ├── SettingsView.axaml/.cs
│   │   ├── AboutWindow.axaml/.cs    # Standalone about window (auto update check)
│   │   ├── SoundPickerWindow.axaml/.cs  # Sound selection modal
│   │   └── Components/
│   │       └── BatteryNotificationSection.axaml/.cs  # Reusable notification UI component
│   ├── App.axaml/.cs                # Theme init + tray setup + startup behaviour
│   ├── Program.cs
│   └── ViewLocator.cs
│
└── BatteryNotifier.Tests/         # xUnit tests (net10.0)
    ├── AppSettingsTests.cs
    ├── BatteryManagerStoreTests.cs
    ├── DebouncerTests.cs
    ├── NotificationMessageTests.cs
    ├── NotificationServiceTests.cs
    ├── NotificationTemplatesTests.cs
    ├── PowerUsageServiceTests.cs    # ps output parsing tests
    └── ProcessTipsTests.cs          # Known app tip resolution tests
```

---

## Build & Run

```bash
# Build the solution
dotnet build BatteryNotifier.sln

# Run tests
dotnet test BatteryNotifier.Tests/

# Run the Avalonia app
dotnet run --project BatteryNotifier.Avalonia/BatteryNotifier.Avalonia.csproj

# Build release
dotnet publish BatteryNotifier.Avalonia/BatteryNotifier.Avalonia.csproj -c Release
```

---

## Key Technologies

| Concern | Library |
|---|---|
| UI Framework | Avalonia 11.3.12 |
| MVVM / Reactive | ReactiveUI + Avalonia.ReactiveUI |
| Icons | IconPacks.Avalonia (Phosphor icon set) |
| Audio (Windows) | NAudio 2.2.1 (`WaveOutEvent` + `AudioFileReader`) |
| Audio (macOS) | `afplay` via `Process` (ArgumentList) |
| Audio (Linux) | `paplay` / `aplay` via `Process` (ArgumentList) |
| Logging | Serilog (Console + File + Debug sinks) |
| Settings | `System.Text.Json` → encrypted at rest (DPAPI on Windows, AES-256-GCM on macOS/Linux) |
| Battery Info | WMI `Win32_Battery` + `Win32_PowerManagementEvent` |

---

## Architecture

### Navigation
Navigation is handled directly in `MainWindowViewModel` — no DI container. The view is swapped by setting `CurrentView`:

```
MainWindowViewModel.CurrentView
  → null                (default = home/battery view)
  → SettingsViewModel   (on gear icon click)
```

Back navigation uses a callback `Action` passed into `SettingsViewModel`. Settings slides in from the right with `CubicEaseOut`, slides out with `CubicEaseIn` (200ms). ViewModel disposal is deferred 250ms so content stays visible during the close animation. Home screen is always rendered underneath (no `IsVisible` binding) — settings overlays on top with a solid background.

### Battery Monitoring Pipeline

```
BatteryInfoProvider (WMI / platform-specific)
  ↓ (1s polling + WMI power events on Windows + Darwin notify on macOS)
BatteryMonitorService
  ↓ BatteryStatusChanged / PowerLineStatusChanged events
  ├── BatteryManagerStore  (shared in-memory state)
  ├── MainWindowViewModel  (updates UI on Dispatcher.UIThread)
  └── NotificationService.PublishNotification()
        ↓ (escalating backoff + throttle + pause check + critical bypass)
      NotificationService.NotificationReceived event
        ↓
      NotificationDisplayService.DeliverNotification()
        ↓ SystemStateDetector.GetSuppressionState()
        ├── [DND/Fullscreen?] → suppress toast + sound (Critical overrides)
        ├── Screen flash + notification card (Avalonia-native)
        └── NotificationManager → SoundManager (audio playback)
```

### Power-Hungry App Detection (Battery Drainers)

```
PowerUsageService (15s active / 2min background polling)
  ├── macOS/Linux: ps -eo pid,%cpu,comm → ParsePsOutput() → TryParsePsLine()
  └── Windows: Process.GetProcesses() → two-snapshot CPU delta
      ↓ FilterAndSort(): exclude self, system noise, <1% CPU → top 5
  ProcessesUpdated event
      ↓
  HealthDashboardViewModel.OnProcessesUpdated()
      ↓ Dispatcher.UIThread.Post()
  BuildDrainersDisplay():
      ↓ FormatBatteryImpact(): time cost > watts > hidden
      ↓ ProcessTips.GetTip(): actionable tip for known apps
      ↓ ComputeDrainersSummary(): battery-centric summary
  TopProcesses → AXAML ItemsControl in HealthBottomSheet
  (visible only when: on battery + has data + has battery metrics)
```

**Display priority** — the card only shows when real battery data exists (power draw or time remaining). Raw CPU% is never shown to users.

| Data Available | Per-Process Display | Summary |
|---|---|---|
| Time remaining + power | `~25min` | "Chrome is costing you ~25min of battery life." |
| Power only | `~6.3W` | "Chrome is draining ~6.3W from your battery." |
| Neither | Card hidden | — |

**Known app tips** (`ProcessTips`): data-driven lookup (exact match dictionary + substring match array) for browsers, communication apps, media, dev tools, and system processes. Tips are actionable: "Close unused tabs", "Quit when not in a call", "Spotlight indexing — will finish soon".

**System process filtering** (`ProcessTips.SystemProcesses`): `FrozenSet<string>` of low-level OS processes (kernel_task, svchost, systemd, etc.) excluded from the drainer list.

### Notification Trigger Rules

- **Full battery**: level >= threshold AND charger plugged in (`PowerLineStatus == Online`)
- **Low battery**: level <= threshold AND not charging
- Unplugging while above full threshold does NOT trigger a notification
- Power state changes reset all notification trackers for eager re-notification

### Notification Escalation (Duolingo-inspired)

Per-tag escalating backoff replaces flat deduplication:
- **Backoff**: immediate → 2 min → 5 min → 10 min → 15 min → 30 min → 45 min → silenced
- **Auto-recovery**: after 2 hours of silence, the tracker resets ("recovering arm")
- **Message templates** (`NotificationTemplates`): vary by battery level tier AND escalation count
- **Power state change**: resets all trackers so notifications fire eagerly again

### Settings Flow

`AppSettings.Instance` is a thread-safe singleton loaded on first access.
Settings are encrypted at rest via `SettingsEncryption` (DPAPI on Windows, AES-256-GCM on macOS/Linux).
All ViewModel property setters call `_settings.Save()` immediately (or throttled 500 ms for sliders).

### Theme

Theme is stored as `ThemeMode` enum (`System` / `Light` / `Dark`) in `AppSettings`. On startup `App.axaml.cs` sets `Application.Current.RequestedThemeVariant`. Theme commands directly set `RequestedThemeVariant`.

### Sound System

Three tiers of sounds, each with a settings prefix:

| Tier | Prefix | Storage | Playback |
|---|---|---|---|
| Built-in synthesized | `builtin:Name` | Generated WAV in `$TMPDIR/BatteryNotifier/sounds/` | Loops until stopped |
| Editor's Choice (bundled) | `bundled:FileName.mp3` | Avalonia resources → extracted to temp cache | Plays once in full |
| Custom (user-imported) | `custom:filename.wav` | Copied to `{AppData}/BatteryNotifier/sounds/` | Plays once in full |

Sound picker groups: Full Battery — Calm, Low Battery — Warning, General, Editor's Choice — Full Battery, Editor's Choice — Low Battery, Custom.

`BuiltInSounds.Resolve()` is the central resolver — delegates to `CustomSoundsLibrary` for `custom:` and to `ExternalResolver` (set by `App.axaml.cs`) for `bundled:`.

### Sound Picker (ReactiveUI Interaction pattern)

`BatteryNotificationSectionViewModel` exposes:
```csharp
public Interaction<(string? SettingsValue, string Title), SoundPickerItem?> OpenSoundPickerInteraction { get; }
```

`BatteryNotificationSection.axaml.cs` registers the handler in `OnDataContextChanged`, creating a `SoundPickerWindow` shown via `ShowLightDismiss()`.

### DND / Fullscreen Suppression

`SystemStateDetector` checks OS state before delivering notifications:

| Platform | DND Detection | Fullscreen Detection |
|---|---|---|
| macOS (Monterey) | `defaults read` doNotDisturb key | AppleScript window size vs screen |
| macOS (Ventura/Sonoma) | `Assertions.json` via plutil | AppleScript window size vs screen |
| macOS (Tahoe+) | Menu bar item description check (read-only, no click) | AppleScript window size vs screen |
| Windows | WNF `NtQueryWnfStateData` (Focus Assist) | P/Invoke `GetForegroundWindow` + `GetWindowRect` |
| Linux | `gsettings` (GNOME) / `dbus-send` (KDE) | `xprop _NET_WM_STATE_FULLSCREEN` / `wmctrl` |

DND monitoring: Darwin `notify_check` every 1s (zero-cost memory read) for instant detection on pre-Tahoe macOS. 5s direct poll fallback for Tahoe+ where Darwin notify for DND was removed. Only runs while window is visible.

macOS Tahoe detection: reads `description of every menu bar item` from ControlCenter process. When Focus is active, macOS shows a "Focus" item. No clicking, no dropdown, no flicker. Requires Accessibility permission — app prompts on first launch via `AXIsProcessTrusted()` check and opens System Settings directly.

Suppression rules: DND suppresses toast + sound. Fullscreen suppresses toast only. Critical priority (battery ≤10% while discharging) bypasses everything including backoff, silencing, throttle, and pause.

---

## AppSettings Reference

Stored at: `%AppData%/BatteryNotifier/appsettings.json` (Windows) / `~/.config/BatteryNotifier/` (Linux) / `~/Library/Application Support/BatteryNotifier/` (macOS)

Encrypted at rest. Windows uses DPAPI (OS-managed, tied to user account). macOS/Linux use AES-256-GCM with key in `.settings.key` (chmod 600). Plaintext legacy files are auto-migrated on first load.

| Property | Default | Description |
|---|---|---|
| `FullBatteryNotification` | `true` | Enable full battery notification |
| `LowBatteryNotification` | `true` | Enable low battery notification |
| `FullBatteryNotificationValue` | `96` | Threshold % to trigger full battery alert |
| `LowBatteryNotificationValue` | `25` | Threshold % to trigger low battery alert |
| `FullBatteryNotificationMusic` | `builtin:Harp` | Sound (`builtin:Name`, `bundled:File`, `custom:File`, or absolute path) |
| `LowBatteryNotificationMusic` | `builtin:Klaxon` | Sound (`builtin:Name`, `bundled:File`, `custom:File`, or absolute path) |
| `StartMinimized` | `true` | Hide to tray on launch |
| `ThemeMode` | `System` | `System` / `Light` / `Dark` |
| `LaunchAtStartup` | `true` | Register in OS startup mechanism |
| `AppId` | `Guid` | Unique app identity |

---

## Security Model

### Defence-in-Depth for Sound Files

```
User picks file (StorageProvider / Import Sound)
  → CustomSoundsLibrary.Import()
      ✓ Extension allowlist (.wav, .mp3, .m4a, .wma, .ogg, .flac, .aac)
      ✓ File exists, ≤ 50 MB, not a symlink
      ✓ Copies to app data dir (atomic write via .tmp + rename)
  → AppSettings.SanitizeSoundPath() on load
      ✓ Allows builtin:, bundled:, custom: prefixes
      ✓ Re-canonicalizes absolute paths via Path.GetFullPath()
      ✓ Rejects non-rooted paths
  → SoundManager.PlaySoundAsync() before playback
      ✓ Canonical path validation
      ✓ Symlink rejection
      ✓ 50 MB size guard
      ✓ ArgumentList (not Arguments string) for subprocess calls
```

### Settings Encryption

- **Windows**: DPAPI (`ProtectedData`) — OS-managed encryption tied to user account, no key file needed
- **macOS/Linux**: AES-256-GCM authenticated encryption (tamper-evident)
- File format: `[12-byte nonce][16-byte tag][ciphertext]`
- Key in `.settings.key` with restrictive OS permissions (chmod 600 / NTFS ACL)
- `CryptographicException` on tamper → reset to defaults
- Atomic write via `.tmp` + `File.Move(overwrite: true)`

### Subprocess Security (ProcessRunner, SystemStateDetector, SoundManager, NotificationPlatformService)

- **ArgumentList** (not Arguments string) for all subprocess calls — prevents argument injection
- **Bounded output** — max 8 KB read from stdout to prevent OOM
- **Enforced timeout** — processes killed after 3s via async read + `ManualResetEventSlim`
- **Input validation** — e.g. xdotool window ID validated as numeric before passing to xprop
- **Stdin-based scripts** — PowerShell/osascript receive scripts via stdin, not command-line args
- **No `org.gnome.Shell.Eval`** — uses read-only D-Bus queries instead of JS eval in compositor

### Notification Sanitization (NotificationPlatformService)

- macOS: `SanitizeForAppleScript()` escapes `\`, `"`, newlines
- Windows: `SanitizeForPowerShell()` strips `$`, backtick; `SanitizeForXml()` escapes `&<>'`
- Linux: `SanitizePlainText()` strips all control characters

---

## Avalonia Patterns Used in This Project

### Conditional CSS classes (NOT WPF-style converters)
```xml
<Button Classes.theme-active="{Binding IsLightTheme}" />
```

### ToggleSwitch without text labels
```xml
<ToggleSwitch IsChecked="{Binding SomeBool}" OnContent="" OffContent="" />
```

### StringConverters for visibility
```xml
<Button IsVisible="{Binding SomePath, Converter={x:Static StringConverters.IsNotNullOrEmpty}}" />
```

### Cross-thread UI update from service events
```csharp
Dispatcher.UIThread.Post(RefreshBatteryStatus);
```

### StringFormat in bindings
```xml
<TextBlock Text="{Binding BatteryPercentage, StringFormat='{}{0:F0}%'}" />
```

### Reusable component pattern
```xml
<components:BatteryNotificationSection DataContext="{Binding FullBatterySection}" />
```

### Smooth image rendering
```xml
<Image RenderOptions.BitmapInterpolationMode="MediumQuality" />
```

---

## NotificationService — Escalating Backoff & Throttling

- **Escalating backoff**: per-tag tracker with intervals [0, 2min, 5min, 10min, 15min, 30min, 45min] → silenced after 7 notifications
- **Auto-recovery**: silenced tags auto-reset after 2 hours
- **Throttle interval**: 2 s — rapid notifications held in `_pendingNotifications`, flushed by one-shot timer
- **Power state change**: resets all trackers via `ResetAllTrackers()`
- **Alert range change**: resets trackers + forces immediate re-check so new thresholds trigger instantly
- **Overlapping alerts**: when multiple alerts trigger simultaneously, only the narrowest range fires
- **Critical priority** (battery ≤10% discharging): bypasses backoff, silencing, throttle, and pause
- **Pause/Resume**: user can pause all non-critical notifications for 2 hours (auto-resumes via `AutoResumeIfExpired()`). Toggled from tray menu or main window banner. `PausedChanged` event syncs UI instantly

---

## Notification Templates

Messages vary by **battery level tier** and **escalation count**:

| Low Battery Tier | Level Range | Tone |
|---|---|---|
| Critical | ≤ 10% | Urgent — "shut down soon", "save your work" |
| Very Low | 11–20% | Firm — "time to find your charger" |
| Mild | 21%+ | Casual — "just a heads up" |

| Full Battery Tier | Level Range | Tone |
|---|---|---|
| Complete | 100% | Direct — "fully charged, unplug now" |
| Nearly Full | 97–99% | Gentle — "almost there" |
| Above Threshold | threshold–96% | Informational — "good to go" |

Each tier has multiple escalation stages with randomized variants per stage.

---

## Tray Icon Behavior

```
Tray Menu:
  Show Window / Hide Window    (label syncs with window visibility + focus state)
  Pause Notifications (2h)     (toggles to "Resume Notifications" when paused)
  ─────────────
  Check for Updates...
  About
  ─────────────
  Exit
```

| Platform | Left-click tray | Right-click tray |
|---|---|---|
| Windows/Linux | Show/hide/activate via `Clicked` handler | Context menu |
| macOS | Context menu (OS enforced — NSStatusItem always shows menu) | Context menu |

**Window show/hide logic** (`OnTrayIconClicked`):
- Hidden → `ShowMainWindow()` (show + activate)
- Visible but behind other apps (`IsActive: false`) → `Activate()` only (no dock icon change)
- Visible and focused (`IsActive: true`) → `HideMainWindow()`

**macOS menu workaround**: opening the tray menu deactivates the window, making `IsActive` unreliable at menu-click time. `_wasVisibleBeforeMenu` captures `IsVisible` state when the label updates (on `IsActive` change), so `OnShowHideWindow` reads the snapshot.

**Main window close button**: cancels close and hides to tray. Skips hide if child dialogs (About, Sound Picker) are open to prevent accidental hide during settings.

---

## About Window

Standalone window (no owner) — can be opened from the tray without the main window visible. Centers on screen. Draggable via entire surface (`PointerPressed` → `BeginMoveDrag`).

Auto-checks for updates on open (Chrome-style): shows "Checking for updates..." → "You're on the latest version" or "Update available: vX.Y.Z" (clickable, opens release page in browser).

---

## Notification Pause

- **Tray menu**: "Pause Notifications (2h)" / "Resume Notifications"
- **Main window banner**: amber banner with bell-slash icon + "Resume" button (instant via `PausedChanged` event)
- **Auto-resume**: after 2 hours, `AutoResumeIfExpired()` runs on next `PublishNotification()` call
- **DND interaction**: pause banner hidden when DND is active (`ShowPausedBanner => IsPaused && !IsDndActive`) — DND already suppresses
- **Critical override**: battery ≤10% bypasses pause

---

## Launch at Startup Implementation

| Platform | Mechanism |
|---|---|
| Windows | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` registry key |
| macOS | `~/Library/LaunchAgents/com.batterynotifier.plist` (loaded via `launchctl`) |
| Linux | `~/.config/autostart/BatteryNotifier.desktop` |

---

## Sound Playback Implementation

| Platform | Method |
|---|---|
| Windows | `NAudio.Wave.AudioFileReader` + `NAudio.Wave.WaveOutEvent` |
| macOS | `afplay` subprocess (ArgumentList) |
| Linux | `paplay` subprocess, falls back to `aplay` (ArgumentList) |

Built-in synthesized tones loop until duration timeout or `StopSound()`. Custom and bundled sounds play once in full.

---

## Battery State → UI Image Mapping

```
BatteryState.Full      → /Assets/FullBattery.png  (≥ 96%)
BatteryState.Adequate  → /Assets/FullBattery.png  (60–95%)
BatteryState.Sufficient→ /Assets/Sufficient.png   (40–59%)
BatteryState.Low       → /Assets/LowBattery.png   (15–39%)
BatteryState.Critical  → /Assets/LowBattery.png   (≤ 14%)
```

---

## Constants

```csharp
// BatteryNotifier.Core/Constants.cs
LowBatteryTag  = "LowBattery"
FullBatteryTag = "FullBattery"
DefaultNotificationTimeout = 3000 ms
ApplicationVersion = resolved from csproj at build time
SourceRepositoryUrl = "https://github.com/Sandip124/BatteryNotifier"
```

---

## Current Branch

`expr/avalonia` — active development branch for the Avalonia port.
Main branch: `master`

---

## Known Limitations / Future Work

- `BatteryInfoProvider` uses WMI — **Windows only**. macOS/Linux battery info needs a cross-platform provider.
- macOS Tahoe DND detection requires Accessibility permission (app prompts on first launch). Without it, DND state is not detected.
- macOS tray icon: left-click always opens context menu (OS limitation). "Show Window" is the first menu item as a workaround.
- macOS external display detection suppresses notifications when charger must stay connected.
- Linux GNOME: no system tray by default (needs AppIndicator extension). Left-click behavior depends on SNI implementation.
- Linux CI builds are currently disabled in the GitHub Actions workflow.
