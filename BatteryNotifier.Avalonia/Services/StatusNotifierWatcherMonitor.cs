using System;
using System.Threading.Tasks;
using BatteryNotifier.Core.Logger;
using Serilog;
using Tmds.DBus.Protocol;

namespace BatteryNotifier.Avalonia.Services;

/// <summary>
/// Tracks whether a StatusNotifierItem <em>host</em> — the thing that actually draws tray icons
/// on Linux — is present on the session bus, and reports when one comes or goes.
///
/// Avalonia's <c>TrayIcon</c> asks <c>org.kde.StatusNotifierWatcher</c> to adopt the icon exactly
/// once, when the icon object is constructed. If nobody owns that name at that instant it logs
/// <c>Interface 'org.kde.StatusNotifierWatcher' is unavailable.</c> and gives up permanently — the
/// app then has no tray icon for the rest of its lifetime. On GNOME the watcher belongs to a shell
/// extension rather than the shell itself, so it is routinely missing right after login (we launch
/// at startup, before extensions finish loading) and vanishes again whenever the shell or the
/// extension reloads. That is what strands a hidden window with no way to bring it back.
///
/// <see cref="TrayIconService"/> subscribes to <see cref="AvailabilityChanged"/> and creates the
/// tray icon the first time a host appears — deliberately create-once; see the remarks on
/// <c>TrayIconService.OnSniHostAvailabilityChanged</c> for why replacing a live icon crashes.
/// </summary>
internal sealed class StatusNotifierWatcherMonitor : IDisposable
{
    private const string DBusService = "org.freedesktop.DBus";
    private const string DBusPath = "/org/freedesktop/DBus";
    private const string WatcherName = "org.kde.StatusNotifierWatcher";

    private static readonly ILogger Logger =
        BatteryNotifierAppLogger.ForContext<StatusNotifierWatcherMonitor>();

    private bool _disposed;

    /// <summary>Raised with true when a host appears, false when the last one goes away.</summary>
    public event Action<bool>? AvailabilityChanged;

    /// <summary>Last known presence, so repeated signals for the same state stay quiet.</summary>
    private bool _isAvailable;

    /// <summary>
    /// Begins watching. Safe to call on non-Linux (does nothing) and safe to fail — a broken
    /// watch just means we keep whatever tray icon Avalonia managed to create on its own.
    /// </summary>
    public void Start()
    {
        if (!OperatingSystem.IsLinux()) return;

        _ = Task.Run(async () =>
        {
            try
            {
                var connection = Connection.Session;
                await connection.ConnectAsync().ConfigureAwait(false);
                await WatchOwnerChangesAsync(connection).ConfigureAwait(false);

                // Query after subscribing, so a host that appears mid-setup can't slip through.
                SetAvailability(await HasOwnerAsync(connection).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Could not monitor {Watcher}; tray icon recovery is disabled",
                    WatcherName);
            }
        });
    }

    private async Task WatchOwnerChangesAsync(Connection connection)
    {
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = DBusService,
            Path = DBusPath,
            Interface = DBusService,
            Member = "NameOwnerChanged",
            Arg0 = WatcherName,
        };

        await connection.AddMatchAsync(
            rule,
            static (Message message, object? _) =>
            {
                var reader = message.GetBodyReader();
                reader.ReadString();                 // name — already filtered by Arg0
                reader.ReadString();                 // previous owner
                return reader.ReadString().Length > 0; // new owner: empty means "gone"
            },
            (Exception? ex, bool hasOwner, object? _, object? _) =>
            {
                if (ex != null)
                {
                    Logger.Warning(ex, "{Watcher} owner-change watch faulted", WatcherName);
                    return;
                }

                SetAvailability(hasOwner);
            }).ConfigureAwait(false);
    }

    private static async Task<bool> HasOwnerAsync(Connection connection)
    {
        return await connection.CallMethodAsync(
            CreateNameHasOwnerMessage(connection),
            static (Message message, object? _) => message.GetBodyReader().ReadBool())
            .ConfigureAwait(false);
    }

    // Split out because MessageWriter is a ref struct and cannot live across an await.
    private static MessageBuffer CreateNameHasOwnerMessage(Connection connection)
    {
        using var writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: DBusService,
            path: DBusPath,
            @interface: DBusService,
            member: "NameHasOwner",
            signature: "s");
        writer.WriteString(WatcherName);
        return writer.CreateMessage();
    }

    private void SetAvailability(bool available)
    {
        if (_disposed || available == _isAvailable) return;

        _isAvailable = available;
        Logger.Information("SNI host {State} ({Watcher})",
            available ? "available" : "gone", WatcherName);

        AvailabilityChanged?.Invoke(available);
    }

    public void Dispose()
    {
        _disposed = true;
        AvailabilityChanged = null;
    }
}
