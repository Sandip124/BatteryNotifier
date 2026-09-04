using BatteryNotifier.Core.Store;
using BatteryNotifier.Core.Utils;

namespace BatteryNotifier.Avalonia;

/// <summary>Playful status/greeting/DND copy for the main window, picked with rotating variety.</summary>
internal static class BatteryPhrases
{
    private static readonly string[] ChargingPhrases =
    [
        "Charging — estimating time to full...",
        "Plugged in — calculating charge time...",
        "Charging up — estimate available soon",
        "Power connected — charging in progress",
    ];

    private static readonly string[] DischargingPhrases =
    [
        "On battery — estimating time remaining...",
        "Running on battery power",
        "Unplugged — calculating battery life...",
        "On battery — estimate available soon",
    ];

    private static readonly string[] GreetingsFull =
    [
        "Your battery is vibing at 100%.",
        "Fully juiced! Time to unplug.",
        "Battery's living its best life.",
        "All topped up. You're golden!",
        "Full tank energy right here."
    ];

    private static readonly string[] GreetingsAdequate =
    [
        "Battery's looking great today!",
        "Smooth sailing ahead.",
        "You've got plenty of juice.",
        "All systems go. Carry on!",
        "Battery says: 'I'm chilling.'"
    ];

    private static readonly string[] GreetingsSufficient =
    [
        "Still going strong!",
        "Halfway there, keep cruising.",
        "Battery's holding steady.",
        "Not bad, not bad at all.",
        "Doing just fine over here."
    ];

    private static readonly string[] GreetingsLow =
    [
        "Getting a bit thirsty...",
        "Maybe find a charger soon?",
        "Battery's sending SOS vibes.",
        "Running on fumes here!",
        "A charger would be nice right about now."
    ];

    private static readonly string[] GreetingsCritical =
    [
        "MAYDAY! Plug in, plug in!",
        "Battery's on life support.",
        "We're in the danger zone!",
        "This is not a drill. Charge me!",
        "Counting down... find power NOW."
    ];

    private static readonly string[] GreetingsCharging =
    [
        "Charging up! Sit tight.",
        "Nom nom nom... delicious electricity.",
        "Sipping on some sweet power.",
        "Refueling in progress...",
        "Getting stronger by the minute!"
    ];

    private static readonly string[] DndMessages =
    [
        "Do Not Disturb is on — notifications are paused.",
        "Focus mode active — notifications won't show.",
        "DND enabled — you won't see battery alerts.",
        "Notifications silenced by Do Not Disturb.",
    ];

    /// <summary>Placeholder phrase shown while a time estimate isn't available.</summary>
    public static string BatteryPhrase(bool isCharging) =>
        Variety.Pick(isCharging ? ChargingPhrases : DischargingPhrases);

    /// <summary>Greeting shown briefly when the window opens.</summary>
    public static string StatusMessage(BatteryState state, bool isCharging)
    {
        if (isCharging) return Variety.Pick(GreetingsCharging);

        var pool = state switch
        {
            BatteryState.Full => GreetingsFull,
            BatteryState.Adequate => GreetingsAdequate,
            BatteryState.Sufficient => GreetingsSufficient,
            BatteryState.Low => GreetingsLow,
            BatteryState.Critical => GreetingsCritical,
            _ => GreetingsAdequate
        };

        return Variety.Pick(pool);
    }

    public static string DndMessage() => Variety.Pick(DndMessages);
}
