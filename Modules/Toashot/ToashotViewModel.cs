using System;
using System.Diagnostics;
using System.FileSystem;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Toashot.Avalonia.Common;

namespace Toashot.Modules.Toashot;

public partial class ToashotViewModel : ObservableObject
{
    public Action? CloseAction { get; set; }
    [ObservableProperty] private string? _filePath;
    [ObservableProperty] private Bitmap? _image;
    [ObservableProperty] private bool _error = false;
    [ObservableProperty] private string _msg = "点击保存，拖动使用";
    public static AppConfig AppConfig => AppConfiger.Config;

    public void SetError(string msg)
    {
        Error = true;
        Msg = msg;
    }
    public ToashotViewModel(string? path, string? err)
    {
        if (err is not null) SetError(err);
        if (path is not null) {
            FilePath = path;
            Image = new Bitmap(path);
        };
    }

    private void SaveImg(out string? output)
    {
        output = null;
        if(Image is null) return;
        if(FilePath is null) return;
        if(!Directory.Exists(AppConfig.Toashot.Path)) Directory.CreateDirectory(AppConfig.Toashot.Path);
        output = Path.Combine(AppConfig.Toashot.Path, Path.GetFileName(FilePath));
        File.Move(FilePath, output, overwrite: true);
    }
    [RelayCommand]
    private void Save()
    {
        SaveImg(out var file);
        if(file is null) return;
        CloseAction?.Invoke();
    }
    [RelayCommand]
    private void Open()
    {
        SaveImg(out var file);
        if(file is null) return;
        Process.Start(new ProcessStartInfo(file)
        {
            UseShellExecute = true
        });
        CloseAction?.Invoke();
    }
    [RelayCommand]
    private void OpenDir()
    {
        SaveImg(out var file);
        if(file is null) return;
        try 
        {
            FileSystemLauncher.OpenFolderAndSelectItem(file);
        }
        catch (Exception)
        {
            FileSystemLauncher.OpenFolder(Path.GetDirectoryName(file)!);
        }
        CloseAction?.Invoke();
    }
    [RelayCommand]
    private async Task Delete()
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
        CloseAction?.Invoke();
    }
}