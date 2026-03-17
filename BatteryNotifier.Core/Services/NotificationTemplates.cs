namespace BatteryNotifier.Core.Services;

/// <summary>
/// Provides varied notification messages based on battery level and escalation count.
/// Each escalation step picks from level-appropriate templates to avoid repetitive wording.
/// </summary>
public static class NotificationTemplates
{
    // ── Low Battery ──────────────────────────────────────────────

    // Level tiers: Critical (≤10%), Very Low (11-20%), Low (21-threshold)
    private static readonly string[][] LowBatteryCritical =
    [
        // Escalation 0 (first notification)
        [
            "Battery at {0}% — critically low! Plug in immediately.",
            "Only {0}% battery remaining. Save your work and charge now!",
            "Critical: {0}% left. Your device may shut down soon.",
        ],
        // Escalation 1
        [
            "Still at {0}% and dropping. Please connect your charger.",
            "Battery hasn't recovered — {0}% left. Charge urgently.",
        ],
        // Escalation 2
        [
            "Battery critical at {0}%! Risk of data loss if not charged.",
            "Urgent: only {0}% power. Plug in now to avoid shutdown.",
        ],
        // Escalation 3 (final before silence)
        [
            "Final warning: {0}% battery. Charging is essential now.",
        ],
    ];

    private static readonly string[][] LowBatteryVeryLow =
    [
        [
            "Battery is at {0}%. Time to find your charger.",
            "Heads up — battery dropped to {0}%.",
            "Battery down to {0}%. Consider plugging in soon.",
        ],
        [
            "Still running on {0}% battery. Plug in when you can.",
            "Battery at {0}% and falling — charger recommended.",
        ],
        [
            "Battery has been at {0}% for a while. Please charge.",
            "Running low at {0}% — plug in to avoid interruption.",
        ],
        [
            "Last reminder: {0}% battery. Charging strongly recommended.",
        ],
    ];

    private static readonly string[][] LowBatteryMild =
    [
        [
            "Battery is at {0}%. You might want to plug in soon.",
            "Just a heads up — battery is at {0}%.",
            "Battery level: {0}%. Keep an eye on it.",
        ],
        [
            "Battery still at {0}%. Consider charging when convenient.",
            "Reminder: battery is {0}%. A charger nearby wouldn't hurt.",
        ],
        [
            "Battery holding at {0}%. Plug in if you'll be away from power.",
            "Still at {0}% — might want to top up before it gets lower.",
        ],
        [
            "Final nudge: battery at {0}%. Charge up when you get a chance.",
        ],
    ];

    // ── Full Battery ─────────────────────────────────────────────

    // Level tiers: Fully Charged (100%), Nearly Full (97-99%), Full (threshold-96%)
    private static readonly string[][] FullBatteryComplete =
    [
        [
            "Battery is at {0}%! You can unplug your charger now.",
            "Fully charged at {0}%! Unplug to help preserve battery health.",
            "Battery topped off at {0}%. Safe to disconnect.",
        ],
        [
            "Still plugged in at {0}%. Unplug to reduce battery wear.",
            "Battery full at {0}% — keeping it plugged in isn't ideal for longevity.",
        ],
        [
            "Battery has been at {0}% for a while. Unplug recommended.",
            "Extended charging at {0}% can degrade battery over time.",
        ],
        [
            "Final reminder: unplug at {0}% to protect battery lifespan.",
        ],
    ];

    private static readonly string[][] FullBatteryNearlyFull =
    [
        [
            "Battery at {0}% — nearly full. You can unplug soon.",
            "Almost there! Battery is at {0}%.",
            "Battery charged to {0}%. Consider unplugging.",
        ],
        [
            "Still charging at {0}%. You can safely unplug now.",
            "Battery at {0}% — unplugging helps preserve battery health.",
        ],
        [
            "Battery has been above {0}% for a while. Unplug recommended.",
            "Reminder: battery at {0}%. Disconnect to reduce wear.",
        ],
        [
            "Final reminder: battery at {0}%. Unplugging helps longevity.",
        ],
    ];

    private static readonly string[][] FullBatteryAboveThreshold =
    [
        [
            "Battery charged to {0}%. You can unplug your charger.",
            "Battery at {0}% — good to go! Consider unplugging.",
            "Charge complete at {0}%. Safe to disconnect.",
        ],
        [
            "Still at {0}% on charger. Unplug to preserve battery health.",
            "Battery holding at {0}% — unplugging is recommended.",
        ],
        [
            "Battery at {0}% for a while now. Unplug to reduce wear.",
            "Reminder: {0}% charged. Disconnect when convenient.",
        ],
        [
            "Final reminder: battery at {0}%. Unplugging helps battery life.",
        ],
    ];

    // ── Public API ───────────────────────────────────────────────

    /// <summary>
    /// Gets a low battery message appropriate for the battery level and escalation count.
    /// </summary>
    /// <param name="level">Current battery percentage (0-100)</param>
    /// <param name="escalation">How many times this tag has notified (0-based)</param>
    public static string GetLowBatteryMessage(int level, int escalation)
    {
        var templates = level switch
        {
            <= 10 => LowBatteryCritical,
            <= 20 => LowBatteryVeryLow,
            _ => LowBatteryMild,
        };

        return PickMessage(templates, escalation, level);
    }

    /// <summary>
    /// Gets a full battery message appropriate for the battery level and escalation count.
    /// </summary>
    /// <param name="level">Current battery percentage (0-100)</param>
    /// <param name="escalation">How many times this tag has notified (0-based)</param>
    public static string GetFullBatteryMessage(int level, int escalation)
    {
        var templates = level switch
        {
            100 => FullBatteryComplete,
            >= 97 => FullBatteryNearlyFull,
            _ => FullBatteryAboveThreshold,
        };

        return PickMessage(templates, escalation, level);
    }

    private static string PickMessage(string[][] templates, int escalation, int level)
    {
        // Clamp escalation to available tiers
        var tierIndex = Math.Min(escalation, templates.Length - 1);
        var tier = templates[tierIndex];

        // Pick a random message from the tier for variety
        var messageIndex = Random.Shared.Next(tier.Length);
        return string.Format(tier[messageIndex], level);
    }
}
