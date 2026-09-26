using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Toashot.Modules.Settings;

public partial class SettingsViewModel : ObservableObject
{
    public Action? CloseAction { get; set; }
}