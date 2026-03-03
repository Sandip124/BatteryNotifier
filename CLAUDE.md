# BatteryNotifier — CLAUDE.md

Cross-platform battery monitoring app built with **Avalonia UI** and **.NET**. Notifies the user when the battery is full or low, runs in the system tray, and supports themes, custom sounds, and startup behaviour.

---

## Solution Structure

```
BatteryNotifier/
├── BatteryNotifier.sln
├── BatteryNotifier.Core/          # Platform-agnostic logic (net8.0)
│   ├── Constants.cs
│   ├── Logger/
│   │   ├── BatteryNotifierLogger.cs
│   │   └── BatteryNotifierLoggerConfig.cs
│   ├── Managers/
│   │   ├── NotificationManager.cs   # Emits notifications + plays sounds
│   │   └── SoundManager.cs          # Cross-platform audio playback
│   ├── Providers/
│   │   └── BatteryInfoProvider.cs   # WMI Win32_Battery query
│   ├── Services/
│   │   ├── AppSettings.cs           # JSON settings singleton
│   │   ├── BatteryMonitorService.cs # Background battery polling + WMI events
│   │   ├── NotificationService.cs   # Priority queue, dedup, throttling
│   │   └── StartupManager.cs        # Cross-platform launch at startup
│   ├── Store/
│   │   └── BatteryManagerStore.cs   # Shared battery state (singleton)
│   └── Utils/
│       └── Debouncer.cs
│
└── BatteryNotifier.Avalonia/      # Avalonia UI app (net10.0)
    ├── Assets/                    # Images, icon
    ├── Services/
    │   ├── NavigationService.cs     # INavigationService (unused — navigation is in-VM)
    │   ├── NotificationPlatformService.cs  # Native OS toast (osascript/powershell/notify-send)
    │   └── TrayIconService.cs       # System tray icon + menu
    ├── ViewModels/
    │   ├── ViewModelBase.cs
    │   ├── MainWindowViewModel.cs   # Hosts CurrentView + IsTopmost
    │   ├── HomeViewModel.cs         # Live battery data + quick notification toggles
    │   └── SettingsViewModel.cs     # All settings with auto-save
    ├── Views/
    │   ├── MainWindow.axaml/.cs
    │   ├── HomeView.axaml/.cs
    │   └── SettingsView.axaml/.cs   # Registers file picker interaction handlers
    ├── App.axaml/.cs                # Theme init + tray setup + startup behaviour
    ├── Program.cs
    └── ViewLocator.cs
```

---

## Build & Run

```bash
# Build the solution
dotnet build BatteryNotifier.sln

# Run the Avalonia app
dotnet run --project BatteryNotifier.Avalonia/BatteryNotifier.Avalonia.csproj

# Build release
dotnet publish BatteryNotifier.Avalonia/BatteryNotifier.Avalonia.csproj -c Release
```

---

## Key Technologies

| Concern | Library |
|---|---|
| UI Framework | Avalonia 11.3.2 |
| MVVM / Reactive | ReactiveUI + Avalonia.ReactiveUI |
| Icons | IconPacks.Avalonia (Phosphor icon set) |
| Audio (Windows) | NAudio 2.2.1 (`WaveOutEvent` + `AudioFileReader`) |
| Audio (macOS) | `afplay` via `Process` |
| Audio (Linux) | `paplay` / `aplay` via `Process` |
| Logging | Serilog (Console + File + Debug sinks) |
| Settings | `System.Text.Json` serialised to `%AppData%/BatteryNotifier/appsettings.json` |
| Battery Info | WMI `Win32_Battery` + `Win32_PowerManagementEvent` |

---

## Architecture

### Navigation
Navigation is handled directly in `MainWindowViewModel` — no DI container. The view is swapped by changing `CurrentView`:

```
MainWindowViewModel.CurrentView
  → HomeViewModel       (default)
  → SettingsViewModel   (on gear icon click)
```

`HomeViewModel.NavigateToSettings` (IObservable<Unit>) is subscribed to in `MainWindowViewModel`. Back navigation uses a callback `Action` passed into `SettingsViewModel`.

### Battery Monitoring Pipeline

```
BatteryInfoProvider (WMI)
  ↓ (poll every 2 min + WMI power events)
BatteryMonitorService
  ↓ BatteryStatusChanged / PowerLineStatusChanged events
  ├── BatteryManagerStore  (shared in-memory state)
  ├── HomeViewModel        (updates UI on Dispatcher.UIThread)
  └── MainWindow / TrayIconService
        ↓
      NotificationService.PublishNotification()
        ↓
      NotificationService.NotificationReceived event
        ↓
      TrayIconService → NotificationPlatformService (native OS toast)
                      → NotificationManager → SoundManager
```

### Settings Flow

`AppSettings.Instance` is a thread-safe singleton loaded on first access.
All ViewModel property setters call `_settings.Save()` immediately (or throttled 500 ms for sliders via `WhenAnyValue().Throttle()`).

### Theme

Theme is stored as `ThemeMode` enum (`System` / `Light` / `Dark`) in `AppSettings`. On startup `App.axaml.cs` sets `Application.Current.RequestedThemeVariant`. When the user changes theme in `SettingsViewModel`, the command directly sets `Application.Current.RequestedThemeVariant`.

### Topmost / Always on Top

`SettingsViewModel.PinToWindow` fires `PinToWindowChanged` event → `MainWindowViewModel.IsTopmost` is updated → `MainWindow.Topmost` is data-bound to `IsTopmost`.

### Sound File Picker (ReactiveUI Interaction pattern)

`SettingsViewModel` exposes:
```csharp
public Interaction<string?, string?> BrowseFullBatterySoundInteraction { get; }
public Interaction<string?, string?> BrowseLowBatterySoundInteraction { get; }
```

`SettingsView.axaml.cs` registers the handlers in `OnDataContextChanged`:
```csharp
vm.BrowseFullBatterySoundInteraction.RegisterHandler(async ctx => {
    var path = await BrowseAudioFile(); // Avalonia StorageProvider
    ctx.SetOutput(path);
});
```

---

## AppSettings Reference

Stored at: `%AppData%/BatteryNotifier/appsettings.json` (Windows) / `~/.config/BatteryNotifier/` (Linux) / `~/Library/Application Support/BatteryNotifier/` (macOS)

| Property | Default | Description |
|---|---|---|
| `FullBatteryNotification` | `true` | Enable full battery notification |
| `LowBatteryNotification` | `true` | Enable low battery notification |
| `FullBatteryNotificationValue` | `96` | Threshold % to trigger full battery alert |
| `LowBatteryNotificationValue` | `25` | Threshold % to trigger low battery alert |
| `FullBatteryNotificationMusic` | `null` | Absolute path to custom sound file |
| `LowBatteryNotificationMusic` | `null` | Absolute path to custom sound file |
| `PinToWindow` | `false` | Always on top |
| `StartMinimized` | `true` | Hide to tray on launch |
| `ThemeMode` | `System` | `System` / `Light` / `Dark` |
| `LaunchAtStartup` | `true` | Register in OS startup mechanism |
| `WindowPositionX/Y` | `0` | Last known window position |
| `AppId` | `Guid` | Unique app identity |

---

## Avalonia Patterns Used in This Project

### Conditional CSS classes (NOT WPF-style converters)
```xml
<!-- Correct Avalonia way to conditionally apply a class from a binding -->
<Button Classes.theme-active="{Binding IsLightTheme}" />
```
Then style it:
```xml
<Style Selector="Button.theme-active">
    <Setter Property="Background" Value="#3878C5" />
</Style>
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
private void OnBatteryStatusChanged(object? sender, BatteryStatusEventArgs e)
{
    Dispatcher.UIThread.Post(RefreshBatteryStatus);
}
```

### StringFormat in bindings
```xml
<TextBlock Text="{Binding BatteryPercentage, StringFormat='{}{0:F0}%'}" />
```

---

## NotificationService — Deduplication & Throttling

- **Deduplication interval**: 30 s — same (tag + message + type) won't fire twice within this window
- **Throttle interval**: 2 s — rapid notifications are held in `_pendingNotifications` and flushed by `NotificationManager`'s 3 s timer
- **Cleanup interval**: 5 min — stale dedup cache and pending queue are cleared

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
| macOS | `afplay "<path>"` subprocess |
| Linux | `paplay "<path>"` (falls back to `aplay`) subprocess |

Loop mode is supported (plays until duration timeout or `StopSound()` is called).

---

## Battery State → UI Image Mapping

```
BatteryState.Full      → /Assets/Full.png      (≥ 96%)
BatteryState.Adequate  → /Assets/Normal.png    (60–95%)
BatteryState.Sufficient→ /Assets/Sufficient.png(40–59%)
BatteryState.Low       → /Assets/Low.png       (15–39%)
BatteryState.Critical  → /Assets/Critical.png  (≤ 14%)
```

---

## Constants

```csharp
// BatteryNotifier.Core/Constants.cs
LowBatteryTag  = "LowBattery"
FullBatteryTag = "FullBattery"
DefaultNotificationTimeout = 3000 ms
ApplicationVersion = "3.1.0"
SourceRepositoryUrl = "https://github.com/Sandip124/BatteryNotifier"
```

---

## Current Branch

`expr/avalonia` — active development branch for the Avalonia port.
Main branch: `master`

---

## Known Limitations / Future Work

- `BatteryInfoProvider` and `BatteryMonitorService` use WMI — **Windows only**. A cross-platform battery provider is needed for macOS/Linux.
- `NavigationService.cs` exists but is **not used** — navigation is handled inline in `MainWindowViewModel`.
- The `NavigationService.cs` file can be removed or wired up if DI is introduced.
- Assets: only `FullBattery.png`, `LowBattery.png`, `Sufficient.png`, and `BatteryNotifierLogo.png` exist. `Full.png`, `Normal.png`, `Low.png`, `Critical.png`, `Unknown.png` are referenced in `HomeViewModel` but not yet added to the Assets folder.
- No unit tests yet.
