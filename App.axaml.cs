using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Toashot.Modules.Settings;
using Toashot.Modules.Toashot;

namespace Toashot;

public partial class App : Application
{
    public App()
    {
        DataContext = this;
    }

    private void OnOpenToashot(object? sender, EventArgs e)
        => new ToashotWindow(new ToashotViewModel()).Show();
    private void OnOpenSettings(object? sender, EventArgs e)
        => new SettingsWindow(new SettingsViewModel()).Show();
    private void OnExit(object? sender, EventArgs e)
        => (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown(0);


    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        base.OnFrameworkInitializationCompleted();
    }
}