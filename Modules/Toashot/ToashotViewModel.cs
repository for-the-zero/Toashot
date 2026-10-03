using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Toashot.Modules.Toashot;

public partial class ToashotViewModel : ObservableObject
{
    public Action? CloseAction { get; set; }
    [ObservableProperty] private Bitmap? _image;
    [ObservableProperty] private string? _error;


    public ToashotViewModel(string? path, string? err)
    {
        Error = err;
        if (path is not null) Image = new Bitmap(path);
    }
}