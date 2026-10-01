using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Avalonia.Controls;
using AutoLaunch;

namespace Toashot.Modules.Settings;

public partial class SettingsViewModel : ObservableObject
{
    public Action? CloseAction { get; set; }
    public TopLevel? TopLevel { get; set; }

    private Task OpenUri(string uri)
        => TopLevel?.Launcher?.LaunchUriAsync(new Uri(uri)) ?? Task.CompletedTask;
    [RelayCommand]
    private Task OpenGithub() => OpenUri("https://github.com/for-the-zero/Toashot");
    [RelayCommand]
    private Task OpenAuthor() => OpenUri("https://ftz.is-a.dev");

    private static readonly SafeAutoLauncher Launcher = new AutoLaunchBuilder().Automatic().BuildSafe();
    [ObservableProperty]
    private bool _isAutoStartup = Launcher.TryGetStatus().enabled;
    partial void OnIsAutoStartupChanged(bool value)
    {
        if (value)
            Launcher.TryEnable();
        else
            Launcher.TryDisable();
    }
};