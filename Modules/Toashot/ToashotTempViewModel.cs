using System;
using System.Collections.ObjectModel;
using System.FileSystem;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Toashot.Avalonia.Common;

namespace Toashot.Modules.Toashot;

public partial class ToashotTempViewModel : ObservableObject, IDisposable
{
    public Action? CloseAction { get; set; }
    private readonly FileSystemWatcher _watcher;
    public ObservableCollection<TempShot> Shots { get; } = new();
    [ObservableProperty] private bool _hasShots = true;
    [ObservableProperty] private int _columns = 3;

    public IClipboard? Clipboard { get; set; }
    public IStorageProvider? Storage { get; set; }

    public void SetColumns(double width)
    {
        _lastWidth = width;
        Columns = Math.Max(1, (int)(width / 190));
        var imgWidth = Math.Max(1, width / Columns - 4);
        foreach (var s in Shots)
        {
            s.ImageHeight = imgWidth * s.Aspect;
            s.CardHeight = s.ImageHeight + 30;
        }
    }

    private double _lastWidth;

    public ToashotTempViewModel()
    {
        Directory.CreateDirectory(ToashotSvc.TempDir);
        _watcher = new FileSystemWatcher(ToashotSvc.TempDir, "*.png");
        _watcher.Created += OnChanged;
        _watcher.Changed += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.Renamed += OnChanged;
        _watcher.EnableRaisingEvents = true;
        Refresh();
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
        => Dispatcher.UIThread.Post(Refresh);
    public void Refresh()
    {
        var files = Directory.EnumerateFiles(ToashotSvc.TempDir, "*.png").ToHashSet();
        for (var i = Shots.Count - 1; i >= 0; i--)
        {
            if (files.Contains(Shots[i].FilePath)) continue;
            Shots[i].Dispose();
            Shots.RemoveAt(i);
        }
        foreach (var f in files)
        {
            var shot = Shots.FirstOrDefault(s => s.FilePath == f);
            if (shot is null)
                Shots.Add(new TempShot(f) { Image = TryDecode(f) });
            else if (shot.Image is null)
                shot.Image = TryDecode(f);
        }
        HasShots = Shots.Count > 0;
        if (_lastWidth > 0) SetColumns(_lastWidth);
        foreach (var s in Shots) { s.Clipboard = Clipboard; s.Storage = Storage; }
    }
    private static Bitmap? TryDecode(string path)
    {
        try { return new Bitmap(path); }
        catch { return null; }
    }
    public void Dispose()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        foreach (var s in Shots) s.Dispose();
    }

    [RelayCommand]
    private void Clear()
    {
        if (Directory.Exists(ToashotSvc.TempDir)) Directory.Delete(ToashotSvc.TempDir, true);
        Directory.CreateDirectory(ToashotSvc.TempDir);
        Refresh();
    }

    [RelayCommand]
    private void OpenFolder()
    {
        FileSystemLauncher.OpenFolder(ToashotSvc.TempDir);
    }
}

public sealed partial class TempShot : ObservableObject, IDisposable
{
    [ObservableProperty] private Bitmap? _image;
    [ObservableProperty] private double _imageHeight = 100;
    [ObservableProperty] private double _cardHeight = 130;
    [ObservableProperty] private double _aspect = 0.75; // 高/宽
    public string FilePath { get; }
    public string FileName => Path.GetFileName(FilePath);
    public IClipboard? Clipboard { get; set; }
    public IStorageProvider? Storage { get; set; }

    public TempShot(string path) => FilePath = path;
    public static AppConfig AppConfig => AppConfiger.Config;

    partial void OnImageChanged(Bitmap? value)
    {
        if (value is { } b) Aspect = (double)b.PixelSize.Height / b.PixelSize.Width;
    }
    public void Dispose() => Image?.Dispose();

    [RelayCommand]
    private async Task Copy()
    {
        if (Image is null || Clipboard is null || Storage is null) return;
        var file = await Storage.TryGetFileFromPathAsync(new Uri(FilePath));
        if (file is null) return;
        await Clipboard.SetFileAsync(file);
        await Clipboard.FlushAsync();
    }
    [RelayCommand]
    private void Save()
    {
        if(Image is null) return;
        if(FilePath is null) return;
        if(!Directory.Exists(AppConfig.Toashot.Path)) Directory.CreateDirectory(AppConfig.Toashot.Path);
        var output = Path.Combine(AppConfig.Toashot.Path, FileName);
        File.Move(FilePath, output, overwrite: true);
    }
    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!File.Exists(FilePath)) return;
        try
        {
            await Emik.Rubbish.MoveAsync(FilePath);
        }
        catch (Exception)
        {
            File.Delete(FilePath);
        }
        Dispose();
    }
}