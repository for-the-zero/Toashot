using System;
using Avalonia.Controls;

namespace Toashot.Modules.Toashot;

public partial class ToashotTempWindow : ShadUI.Window
{
    public ToashotTempWindow()
    {
        InitializeComponent();
        ShotList.SizeChanged += (_, e) =>
        {
            if (DataContext is ToashotTempViewModel vm) vm.SetColumns(e.NewSize.Width);
        };
    }

    public ToashotTempWindow(ToashotTempViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseAction = Close;
        Opened += (_, _) =>
        {
            // 必须等窗口挂上 visual root，Clipboard/StorageProvider 这时才可用
            viewModel.Clipboard = Clipboard;
            viewModel.Storage = StorageProvider;
            viewModel.Refresh();
        };
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (DataContext is ToashotTempViewModel vm) vm.Dispose();
    }
}
