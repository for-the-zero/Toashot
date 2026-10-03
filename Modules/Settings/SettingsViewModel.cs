using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AutoLaunch;
using global::Toashot.Avalonia.Common;

namespace Toashot.Modules.Settings;

public partial class SettingsViewModel : ObservableObject
{
    public Action? CloseAction { get; set; }
    public TopLevel? TopLevel { get; set; }

    public static AppConfig AppConfig => AppConfiger.Config;
    private static readonly SafeAutoLauncher Launcher = new AutoLaunchBuilder().Automatic().BuildSafe();

    private Task OpenUri(string uri)
        => TopLevel?.Launcher?.LaunchUriAsync(new Uri(uri)) ?? Task.CompletedTask;
    [RelayCommand]
    private Task OpenGithub() => OpenUri("https://github.com/for-the-zero/Toashot");
    [RelayCommand]
    private Task OpenAuthor() => OpenUri("https://ftz.is-a.dev");

    
    [ObservableProperty]
    public partial bool IsAutoStartup { get; set; } = Launcher.TryGetStatus().enabled;
    partial void OnIsAutoStartupChanged(bool value)
    {
        if (value)
            Launcher.TryEnable();
        else
            Launcher.TryDisable();
    }

    [RelayCommand]
    private async Task ToashotPathBtn()
    {
        if(TopLevel == null) return;
        var folders = await TopLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "选择保存目录",
                SuggestedStartLocation = await TopLevel.StorageProvider.TryGetFolderFromPathAsync(AppConfig.Toashot.Path),
                AllowMultiple = false,
            }
        );
        if(folders.Count <= 0) return;
        string? path = folders[0].TryGetLocalPath();
        if(path == null) return;
        AppConfig.Toashot.Path = path;
    }

    [ObservableProperty]
    public partial string? DraftShortcut { get; set; } = AppConfig.Toashot.Shortcut;
    [ObservableProperty]
    public partial string? DraftFastShot { get; set; } = AppConfig.Toashot.FastShot;
    [ObservableProperty]
    public partial string? HotkeyMessage { get; set; }
    [RelayCommand]
    private void ApplyHotkey()
    {
        if(!TryGesture(DraftShortcut, out var shot)) return;
        if(!TryGesture(DraftFastShot, out var fast)) return;
        if(shot.Key == fast.Key && shot.KeyModifiers == fast.KeyModifiers)
        {
            Fail("两个快捷键不能相同");
            return;
        }
        AppConfig.Toashot.Shortcut = HotkeyBox.Serialize(shot);
        AppConfig.Toashot.FastShot = HotkeyBox.Serialize(fast);
        HotkeyMessage = "已应用";
    }
    private bool TryGesture(string? text, out KeyGesture gesture)
    {
        gesture = null!;
        if(string.IsNullOrWhiteSpace(text)) return Fail("快捷键不能为空");
        try
        {
            gesture = KeyGesture.Parse(text);
        }
        catch(FormatException)
        {
            return Fail($"无法识别「{text}」");
        }
        if(gesture.KeyModifiers == KeyModifiers.None && IsTypingKey(gesture.Key) && !IsDedicatedKey(gesture.Key))
            return Fail($"「{text}」会在打字时反复触发，请加 Ctrl / Alt / Shift / Win");
        return true;
    }
    private static bool IsTypingKey(Key key)
        => key is >= Key.A and <= Key.Z or >= Key.D0 and <= Key.D9
            or Key.Space or Key.Enter or Key.Back or Key.Delete or Key.Tab;
    private static bool IsDedicatedKey(Key key) => key == Key.PrintScreen;
    private bool Fail(string message)
    {
        HotkeyMessage = message;
        return false;
    }
};