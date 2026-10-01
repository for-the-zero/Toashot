using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Toashot.Modules.Toashot;

public partial class ToashotTempViewModel : ObservableObject
{
    public Action? CloseAction { get; set; }
}