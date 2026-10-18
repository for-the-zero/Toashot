using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Toashot.Avalonia.Common;
using Toashot.Modules.Settings;
using Toashot.Modules.Toashot;

namespace Toashot;

public partial class App : Application
{
    public App()
    {
        DataContext = this;
    }

    private ToashotTempWindow? _toashotTempWindow;
    private SettingsWindow? _settingsWindow;

    private void OnOpenToashotT(object? sender, EventArgs e)
    {
        if (_toashotTempWindow is not null)
        {
            _toashotTempWindow.Activate();
            return;
        }
        _toashotTempWindow = new ToashotTempWindow(new ToashotTempViewModel());
        _toashotTempWindow.Closed += (_, _) => _toashotTempWindow = null;
        _toashotTempWindow.Show();
    }
    private void OnOpenSettings(object? sender, EventArgs e)
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }
        _settingsWindow = new SettingsWindow(new SettingsViewModel());
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }
    private void OnExit(object? sender, EventArgs e)
    {
        Listener.Instance.Stop();
        (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown(0);
    }


    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
    public override void OnFrameworkInitializationCompleted()
    {
        var toashotTItem = new NativeMenuItem("查看截图暂存区");
        toashotTItem.Click += OnOpenToashotT;
        var settingsItem = new NativeMenuItem("设置");
        settingsItem.Click += OnOpenSettings;
        var exitItem = new NativeMenuItem("退出");
        exitItem.Click += OnExit;
        TrayIcon.SetIcons(this, new TrayIcons
        {
            new TrayIcon
            {
                Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Toashot/Assets/icon.ico"))),
                ToolTipText = "Toashot",
                Menu = new NativeMenu
                {
                    Items =
                    {
                        toashotTItem,
                        new NativeMenuItemSeparator(),
                        settingsItem,
                        exitItem,
                    }
                }
            }
        });

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            ToashotSvc.Start();
        }

        base.OnFrameworkInitializationCompleted();
    }
}