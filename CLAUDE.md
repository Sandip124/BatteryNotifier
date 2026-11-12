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
│   │   ├── AlertEvaluationService.cs # "When to (re)notify" brain: entry / rapid drop / severity-capped backoff / engagement
│   │   ├── AppSettings.cs           # Encrypted settings singleton (DPAPI / AES-GCM)
│   │   ├── BatteryMonitorService.cs # 1s polling + WMI/Darwin events
│   │   ├── NotificationService.cs   # Delivery pipe: pause, 2s dedup, priority queue
│   │   ├── NotificationTemplates.cs # Level-aware + escalation-aware message templates
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
│   │   ├── HealthDashboardViewModel.cs  # Battery health for bottom sheet
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
    └── NotificationTemplatesTests.cs
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
  └── PublishAlertNotifications()
        ↓ AlertEvaluationService.EvaluateAlerts()   ← the "when to (re)notify" brain
        │    (entry / rapid drop / severity-capped backoff / engagement)
        ↓ NotificationService.PublishNotification()  ← delivery pipe
        │    (pause drop + 2s rapid-fire dedup + priority emit)
      NotificationService.NotificationReceived event
        ↓
      NotificationDisplayService.DeliverNotification()
        ↓ SystemStateDetector.GetSuppressionState()
        ├── [DND/Fullscreen?] → suppress toast + sound (Critical overrides)
        ├── Screen flash + notification card (Avalonia-native)
        │     └── on dismiss → AlertEvaluationService.RecordDismissal(tag, userInitiated)
        └── NotificationManager → SoundManager (audio playback)
```

**Two clear responsibilities:** `AlertEvaluationService` decides *whether/when* an alert should fire (battery-aware, per alert). `NotificationService` is a generic *delivery pipe* — it no longer owns any escalation state.

### Notification Trigger Rules

An alert only fires when the level is inside its range **and** the charger state matches its
`AlertTone` (see `AlertEvaluationService.IsInsideAlertRange`) — the tone is the single classifier:

- **Full** tone alert → only while **plugged in** (`PowerLineStatus == Online`) — an "unplug now" reminder.
- **Low** tone alert → only while **unplugged** — a "plug in" reminder (gated on plugged/unplugged, not "actively charging", so a plugged-but-not-charging battery won't nag).
- **Neutral** tone (custom / mid-range) → **generic**, fires regardless of the charger.
- Power state changes reset all alert state (`ResetAll`) for eager re-notification.

### Notification Re-notify Logic (AlertEvaluationService — the brain)

Alerts are **not** just edge-triggered; while the battery stays inside an alert's range,
`AlertEvaluationService` decides when to re-notify from four inputs (fire if any apply):

1. **Entry** — fires once when the battery crosses into the range (resets the cycle).
2. **Rapid drop** — a ≥5% fall since the last alert (fast-draining / degraded battery).
3. **Severity-capped backoff** — `interval = min(escalating[2→5→10→15→30→45 min], severityCap)`
   where `severityCap` = 2 min (≤10%), 5 min (≤20%), 15 min (else). The cap means it never
   goes silent for hours, and it can't grow past the cap.
4. **Engagement** (`RecordDismissal`) — a **user dismissal** keeps the escalation growing (nag
   less); an **ignored/timed-out** alert resets the cycle so reminders stay eager. Previews
   pass no tag, so they never affect escalation.

- **Message templates** (`NotificationTemplates`): vary by battery level tier AND `GetEscalationCount`.
- **Power state change**: `ResetAll()` clears all per-alert state so alerts fire eagerly again.
- **Overlapping alerts**: when several trigger at once, only the **narrowest** range fires
  (`BatteryMonitorService.SelectNarrowestAlert`).

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

Suppression rules: DND suppresses toast + sound. Fullscreen suppresses toast only. Critical priority (battery ≤10% while discharging) bypasses everything including the re-notify interval, throttle, pause, and DND.

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
| `AlertVolume` | `100` | Alert sound volume 0–100 (0 = muted, no sound played) |
| `AcAlerts` | `true` | Re-fire alerts on charger plug/unplug (global) |
| `NotificationPosition` | `TopCenter` | On-screen notification card position |
| `ScreenFlashEnabled` | `true` | Screen-edge glow flash on notification |
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

## Two-layer notification design

The "when" and the "how" are deliberately separate. Keep new logic on the correct side.

### AlertEvaluationService — the "when to (re)notify" brain (battery-aware, per alert)

- **Re-notify triggers** (fire if any): entry into range, ≥5% rapid drop, or severity-capped
  escalating interval — see [Notification Re-notify Logic](#notification-re-notify-logic-alertevaluationservice--the-brain).
- **Per-alert state**: `WasInside`, `FireCount`, `LastFireLevel`, `LastFireTime` (2% debounce on exit).
- **`GetEscalationCount(id)`**: prior-notifications count for message-template selection.
- **`RecordDismissal(id, userInitiated)`**: engagement feedback — user-dismiss keeps escalating, ignored resets.
- **`ResetAll()`**: clears all per-alert state (called on power-line change / alert-range change).
- **Test seam**: internal `Clock` for deterministic interval tests.

### NotificationService — the delivery pipe (generic, no escalation state)

- **Pause/Resume**: drops non-critical notifications while paused (2 h default, auto-resumes via `AutoResumeIfExpired()`). Toggled from tray menu or main-window banner; `PausedChanged` syncs UI instantly.
- **Throttle interval**: 2 s — rapid-fire bursts coalesced per tag in `_pendingNotifications`, flushed by a one-shot timer (keeps the latest).
- **Priority queue**: emits highest-priority first via `NotificationReceived`.
- **`ResetAllTrackers()`**: now just discards queued/pending notifications (stale-toast guard); the escalation reset lives in `AlertEvaluationService.ResetAll()`.
- **Critical priority** (battery ≤10% discharging): bypasses throttle, pause, and DND.

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

### Alert tone (message + color + charger gating consistency)

`BatteryAlert.Tone` (`AlertTone.Low` / `Full` / `Neutral`) is the **single** classifier for the message wording (`NotificationTemplates.GetAlertMessage`), the accent/flash color (`NotificationDisplayService.DetermineColor`), **and** the charger gate (`AlertEvaluationService.IsInsideAlertRange`), so they never disagree — including for wide, custom, or overlapping ranges:

- **Low** — reaches empty (`LowerBound ≤ 5`) or sits in the low half (`UpperBound ≤ 50`) → low wording, amber/red (red ≤ 10%), fires only while **unplugged**.
- **Full** — reaches full (`UpperBound ≥ 95`) or sits in the high half (`LowerBound ≥ 50`) → full wording, green, fires only while **plugged in**.
- **Neutral** — spans both extremes (e.g. `0–100`) or neither (mid, e.g. `20–80`) → neutral wording, level-based color, **generic** (fires regardless of charger).

A per-alert `FlashColor` (if set) always overrides the auto color. When ranges **overlap**, only the **narrowest** triggered alert fires (`BatteryMonitorService.PublishAlertNotifications` → `MinBy(UpperBound − LowerBound)`); e.g. full `80–100` (width 20) wins over low `0–85` (width 85) in their 80–85% overlap.

---

## Tray / Flyout Window Model

The window behaves like a taskbar/menu-bar **flyout** (JetBrains Toolbox style): the tray/menu-bar icon opens it, and it auto-hides when focus leaves the app. It stays out of the taskbar (`ShowInTaskbar = false`) — except on Linux, where it is `true` so the window stays reachable from the dock (see the hide path below).

| Platform | Left-click icon | Right-click icon |
|---|---|---|
| Windows | Toggle window (Avalonia `TrayIcon.Clicked`) | Native tray context menu |
| Linux | **Activate** window — show + raise + focus, never hide (see below) | Native tray context menu |
| macOS | Toggle window (native `NSStatusItem`) | Native context menu (`NSStatusItem`) |

Menu items: Pause/Resume Notifications, Check for Updates, About, Exit. **Linux additionally gets an "Open BatteryNotifier" item at the top** — on GNOME the shell swallows a single left-click to open this menu and only forwards a *double*-click to us as `Activate`, so the menu is the one gesture that reliably brings the window back. Windows/macOS need no such item (their icon click reaches the app in-process). The item always shows and raises, never toggles: opening the panel menu has already deactivated the flyout, so the window is off screen by the time it fires.

### Why Linux clicks are different

A Linux tray icon is only a StatusNotifierItem D-Bus service: the icon widget and all its click handling live in the panel/shell process (`gnome-shell`, KDE Plasma, …), not ours, on X11 and Wayland alike — there is no in-process raw-click channel like macOS's `NSStatusItem`. On GNOME's `ubuntu-appindicators` (`indicatorStatusIcon.js`) a left-click with a menu attached is held ~400 ms for a possible second click and then opens the menu itself; only a genuine **double**-click reaches our `Activate` (what raises `TrayIcon.Clicked`). Dropping the menu does not help — with no menu an isolated single click reaches nothing at all, and right-click loses Pause/Updates/About/Exit. Hence the "Open BatteryNotifier" item: on GNOME it is the one gesture that reliably reaches the window. Panels that do call `Activate` on a plain left-click (KDE Plasma and friends) need no extra step.

**Tray click handling** (`OnTrayIconClicked`): on Linux always `ShowMainWindow()`; elsewhere on screen → `HideMainWindow()`, off screen → `ShowMainWindow()`. "On screen" is `MainWindow.IsOnScreen` (`IsVisible && WindowState != Minimized`), **not** `IsVisible` — Linux hides by minimizing, so `IsVisible` stays `true` the whole time, and a plain `IsVisible` test made every click take the *hide* branch, leaving a hidden window impossible to reopen.

Linux activates rather than toggles because the icon is a control-center indicator and the window can be on screen but buried behind others, where hiding it is never what the click meant. `MainWindow.BringToFront()` does the raising: `Activate()` alone is only a request, and on X11 it becomes a `_NET_ACTIVE_WINDOW` message that mutter routinely refuses under focus-stealing prevention (the click went to the shell's panel, not to us), leaving the window unfocused behind others. A brief `Topmost` bump (200 ms, then dropped so the window is never left pinned) forces the raise — verified by the window reaching `_NET_WM_STATE_FOCUSED`.

**macOS uses a custom native `NSStatusItem`** (`Services/MacStatusItem.cs`, Objective-C interop) instead of Avalonia's `TrayIcon`. Avalonia's cross-platform `TrayIcon` forces "menu on click" on macOS and never fires `Clicked`, which makes single-click-to-open impossible. `MacStatusItem` wires the status-bar button's target/action directly: a runtime-created `BNStatusItemTarget` ObjC class receives clicks, distinguishes left vs right/control-click via `[NSApp currentEvent]`, toggles the window on left-click, and pops a native `NSMenu` (built from `TrayIconService.BuildMacMenu`, dispatched by tag via `HandleMacMenuSelection`) on right-click. The button image is sized to the status-bar thickness. `Install()` is best-effort and returns false on any failure, so `TrayIconService` falls back to the Avalonia `TrayIcon` (menu-driven). When the native item is used, the Avalonia `TrayIcon` is not created.

### Linux tray survivability (`StatusNotifierWatcherMonitor`)

A Linux tray icon is only a StatusNotifierItem D-Bus service; an SNI **host** (`org.kde.StatusNotifierWatcher`) has to adopt it. Avalonia asks the host to adopt the icon exactly once, when `TrayIcon` is constructed, and if no host owns that name at that instant it logs `Interface 'org.kde.StatusNotifierWatcher' is unavailable.` and gives up permanently — no icon for the rest of the process. On GNOME the host belongs to a *shell extension*, so it is routinely missing right after login (we launch at startup, before extensions load) and disappears again whenever the shell or extension reloads.

`Services/StatusNotifierWatcherMonitor.cs` (Linux only) watches `NameOwnerChanged` for that name over `Tmds.DBus.Protocol` and raises `AvailabilityChanged`. On Linux `TrayIconService` therefore **defers constructing the `TrayIcon` until a host exists** instead of creating it at startup; every other platform still creates it immediately.

**Create once — never destroy and rebuild a live `TrayIcon`.** Both instances claim the same `org.kde.StatusNotifierItem-{pid}-0` bus name, and the old icon's asynchronous teardown releases the name the new one just claimed. The host then rejects the registration (`org.gnome.gjs.JSError.ValueError: Impossible to register an indicator for parameters ...`), Avalonia rethrows that D-Bus error on the dispatcher, and **the process terminates**. Re-registration after a host restart is Avalonia's own job — it watches the name too.

`InstallTrayDBusCrashGuard()` is the backstop for that whole class of failure: a `Dispatcher.UIThread.UnhandledException` handler that marks `DBusException`s thrown from Avalonia's `DBusTrayIconImpl` as handled, so a shell that rejects or races a registration can never take the app down. Every other exception keeps the normal crash path.

Because a host may never appear at all (stock GNOME has no tray), **the window must never depend on the tray to be reachable** — hence Linux hides by minimizing (below).

**Flyout auto-hide** (`MainWindow.axaml.cs`): on `Deactivated`, a deferred (150 ms grace) check hides to tray **only if** focus truly left the application. Guards, in order: window still visible & not re-focused → not within post-show settle (`NotifyShown()`, 500 ms) → no owned windows (Sound Picker / file dialog) → `AppFocusTracker.IsApplicationFocused()` is not true.
- `AppFocusTracker` (`Services/AppFocusTracker.cs`) is **application**-level, not window-level, so opening our own child windows or (macOS) status menu does not trigger a hide: Windows = foreground window's PID == our PID; macOS = `NSApp.isActive`; Linux = `null` → fall back to "any of our windows active".
- Re-checked when the About window or Sound Picker closes (via `ScheduleAutoHideCheck()`), catching "opened a child window, then switched apps".
- **Linux extra guard**: a window is never auto-hidden until it has been `Activated` at least once since it was shown (`_activatedSinceShown`, reset in `HideToTray()`). GNOME/Wayland often maps a programmatically shown window without giving it focus, and the stray `Deactivated` that follows would otherwise hide the window the user just reopened from the tray.

**Shared hide path**: `MainWindow.HideToTray()` (hide + hide Dock icon + enter efficiency mode) is the single place that hides the window — used by the auto-hide, the tray click (`HideMainWindow`), and the close (X) button (`App.axaml.cs` `Closing`).

- **Windows/macOS** hide by unmapping (`Hide()`).
- **Linux** hides by **minimizing** (`WindowState = Minimized`), matching how `App.axaml.cs` already starts up there. `ShowInTaskbar` is `true` on Linux, so a minimized window stays in the dock and Alt-Tab and one click on the dock icon restores it — the window stays reachable even when no SNI host exists and there is no tray icon at all.
- **Restoring on Linux needs an unmap/map cycle**: mutter ignores a plain `Minimized → Normal` flip on a still-mapped window and leaves it stuck with `_NET_WM_STATE_HIDDEN`, so `ShowMainWindow()` calls `Hide()` before setting `Normal` and `Show()`.
- **That remap is not conditioned on `WindowState` on Linux** (`needsRemap = !IsActive && OwnedWindows.Count == 0`). The WM iconifies windows behind Avalonia's back — minimizing from the dock, or a WM shortcut — leaving `WindowState` reporting `Normal` for a window that is not on screen, where `Show()`/`Activate()` alone do nothing at all. Remapping whenever the window is not already focused covers every case; it is skipped while a dialog is open, since unmapping a parent would take its modal child with it.

**Main window close button**: cancels close and hides to tray. Skips hide if child dialogs (About, Sound Picker) are open (`OwnedWindows.Count > 0`) to prevent accidental hide during settings.

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

**Volume** (`AppSettings.AlertVolume`, 0–100) is applied per backend in `SoundManager.PlaySoundAsync(volumePercent)`: `afplay -v` (macOS), `AudioFileReader.Volume` (Windows), and `--volume`/`-volume` for `paplay`/`pw-play`/`mpv`/`ffplay` (Linux). **0 = muted** — playback is skipped entirely (universal, incl. `aplay` which has no volume flag). The sound-picker audition always plays at full volume so sounds can be previewed even when alerts are muted.

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
- macOS tray icon: uses a custom native `NSStatusItem` (`MacStatusItem.cs`) so single-click toggles the window and right-click shows a native menu. Avalonia's `TrayIcon` can't do this on macOS. Falls back to the Avalonia tray icon if native install fails.
- macOS external display detection suppresses notifications when charger must stay connected.
- Linux GNOME: no system tray by default (needs AppIndicator extension). Left-click behavior depends on the SNI host — on `ubuntu-appindicators` a plain single click opens the menu instead of toggling the window; only a double-click reaches `Clicked` (see Tray/Flyout Window Model above). Other panels (KDE Plasma, etc.) may call `Activate` on a plain click. Because no tray icon can be guaranteed, the dock (minimize-to-dock) is the dependable way back to the window on Linux.
- Linux CI builds are currently disabled in the GitHub Actions workflow.
