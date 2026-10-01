using System;
using System.ComponentModel;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Toashot.Avalonia.Common;

public static class AppConfiger
{
    public const string ConfigFileName = "config.json";
    public const string StateFileName = "state.json";

    public static string BaseDirectory => AppContext.BaseDirectory;
    public static string ConfigFilePath => Path.Combine(BaseDirectory, ConfigFileName);
    public static string StateFilePath => Path.Combine(BaseDirectory, StateFileName);

    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static T Read<T>(string filePath) where T : class, new()
    {
        if (!File.Exists(filePath))
            return new T();
        var json = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(json))
            return new T();
        return JsonSerializer.Deserialize<T>(json, Options) ?? new T();
    }

    public static void Write<T>(string filePath, T value)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(value, Options);
        File.WriteAllText(filePath, json);
    }

    private static AppConfig? _config;
    private static PersistentState? _state;

    public static AppConfig Config => _config ??= LoadAndWatch<AppConfig>(ConfigFilePath);
    public static PersistentState State => _state ??= LoadAndWatch<PersistentState>(StateFilePath);

    private static T LoadAndWatch<T>(string filePath) where T : class, INotifyPropertyChanged, new()
    {
        var value = Read<T>(filePath);
        void Save(object? _, PropertyChangedEventArgs __) => Write(filePath, value);

        value.PropertyChanged += Save;
        foreach (var p in typeof(T).GetProperties())
            if (p.GetValue(value) is INotifyPropertyChanged child)
                child.PropertyChanged += Save;
        return value;
    }
}


/// <summary>配置文件</summary>
public partial class AppConfig : ObservableObject
{
    [ObservableProperty] private GlobalSettings _global = new();
    [ObservableProperty] private ToashotSettings _toashot = new();
    public partial class GlobalSettings : ObservableObject
    {
        [ObservableProperty] private string _lang = "zh-CN"; // zh-CN, en
        // [ObservableProperty] private bool _autostart = false;
    }

    public partial class ToashotSettings : ObservableObject
    {
        [ObservableProperty] private string _path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        [ObservableProperty] private string _shortcut = "PrintScreen";
        [ObservableProperty] private string _fastShot = "Alt+PrintScreen";
    }
}
public partial class PersistentState : ObservableObject
{
    //
}