using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Toashot.Modules.Toashot;

public partial class ToashotViewModel : ObservableObject
{
    public Action? CloseAction { get; set; }
}