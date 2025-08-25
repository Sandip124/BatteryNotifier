using System;
using System.IO;
using System.Reflection.Metadata;
using Avalonia.Controls;
using BatteryNotifier.Avalonia.ViewModels;
using BatteryNotifier.Core;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Services;
using BatteryNotifier.Core.Utils;
using Microsoft.VisualBasic.CompilerServices;
using Serilog;

namespace BatteryNotifier.Avalonia.Views;

public partial class MainWindow : Window
{
    private readonly ILogger _logger;
    private Debouncer _debouncer;

    public MainWindow()
    {
        _debouncer = new Debouncer();
        InitializeComponent();
        _logger = BatteryNotifierAppLogger.ForContext<MainWindow>();
    }

    private void InitializeServices()
    {
        // Subscribe to battery monitor events
        BatteryMonitorService.Instance.BatteryStatusChanged += OnBatteryStatusChanged;
        BatteryMonitorService.Instance.PowerLineStatusChanged += OnPowerLineStatusChanged;

        // Subscribe to notifications
        NotificationService.Instance.NotificationReceived += OnNotificationReceived;

        // _themeService = new ThemeChangeService();
        // _themeService.ThemeChanged += OnThemeChanged;
    }

    private void OnBatteryStatusChanged(object sender, BatteryStatusEventArgs e)
    {
        RefreshBatteryStatusIfTabSelected();

        (string message, NotificationType notificationType, string Tag) notificationInfo;
        if (e is { IsCharging: false, IsLowBattery: true })
            notificationInfo = (message: "🔋 Low Battery, please connect to charger.", NotificationType.Global,
                Tag: Constants.LowBatteryTag);
        else if (e is { IsCharging: true, IsFullBattery: true })
            notificationInfo = (message: "🔋 Full Battery, please unplug the charger.", NotificationType.Global,
                Tag: Constants.FullBatteryTag);
        else
            throw new ArgumentOutOfRangeException(nameof(e));

        NotificationService.Instance.PublishNotification(new NotificationMessage()
        {
            Message = notificationInfo.message,
            Type = notificationInfo.notificationType,
            Tag = notificationInfo.Tag
        });
    }

    private void OnPowerLineStatusChanged(object sender, BatteryStatusEventArgs e)
    {
        RefreshBatteryStatusIfTabSelected();
    }


    private void RefreshBatteryStatusIfTabSelected()
    {
        // UtilityHelper.SafeInvoke(AppTabControl, () =>
        // {
        //     if (AppTabControl.SelectedTab == DashboardTab)
        //     {
        //         _batteryManager.RefreshBatteryStatus();
        //         requirePendingBatteryUiUpdate = false;
        //     }
        //     else
        //     {
        //         requirePendingBatteryUiUpdate = true;
        //     }
        // });

        // refresh only when on home page
    }

    private void OnNotificationReceived(object sender, NotificationMessage notification)
    {
        NotificationText.Text = notification.Message;
        _ = _notificationManager.EmitGlobalNotification(notification);
        _debouncer.Debounce(() =>
        {
            if (!NotificationText.IsDisposed)
            {
                NotificationText.Text = string.Empty;
            }
        });
    }

    public string BrowseForSoundFile(string soundFilePath)
    {
        string outputFileName = soundFilePath;

        try
        {
            // using var fileBrowser = new OpenFileDialog();
            //
            // fileBrowser.DefaultExt = "wav";
            // fileBrowser.Filter =
            //     @"Audio files (*.wav;*.mp3;*.m4a;*.wma)|*.wav;*.mp3;*.m4a;*.wma|Wave files (*.wav)|*.wav|MP3 files (*.mp3)|*.mp3|All files (*.*)|*.*";
            // fileBrowser.Title = @"Select Sound File";
            // if (fileBrowser.ShowDialog() == DialogResult.OK)
            // {
            //     var newFileName = fileBrowser.FileName;
            //
            //     if (!UtilityHelper.IsValidWavFile(newFileName))
            //     {
            //         outputFileName = Path.ChangeExtension(newFileName, ".wav");
            //
            //         using var reader = new MediaFoundationReader(newFileName);
            //         WaveFileWriter.CreateWaveFile(outputFileName, reader);
            //     }
            //     else
            //     {
            //         outputFileName = newFileName;
            //     }
            // }
        }
        catch (Exception e)
        {
            _logger.Error(e, " An error occurred while browsing for sound file.");
            NotificationService.Instance.PublishNotification("💀 Error while browsing for sound file!",
                NotificationType.Inline);
            return string.Empty;
        }

        return !string.IsNullOrEmpty(outputFileName) && File.Exists(outputFileName) ? outputFileName : string.Empty;
    }
}