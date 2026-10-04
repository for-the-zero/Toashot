using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Toashot.Modules.Toashot;

public partial class ToashotWindow : Window
{
    private static readonly TimeSpan Anim = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan Life = TimeSpan.FromSeconds(5);

    private readonly DispatcherTimer _timer;
    private bool _closing;

    private PointerPressedEventArgs? _press;
    private Point _pressPos;
    private bool _dragging;

    public ToashotWindow()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = Life };
        _timer.Tick += (_, _) => _ = CloseAsync();
        PointerEntered += (_, _) => _timer.Stop();
        PointerExited += (_, _) => { _timer.Stop(); _timer.Start(); };
    }

    public ToashotWindow(ToashotViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseAction = () => _ = CloseAsync();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        SetVisual(0, 0.92);
    }
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        SetVisual(1, 1);
        _timer.Start();
        HideFromAltTab();
    }
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (DataContext is ToashotViewModel vm) vm.Image?.Dispose();
    }
    public async Task CloseAsync()
    {
        if (_closing) return;
        _closing = true;
        _timer.Stop();
        SetVisual(0, 0.92);
        await Task.Delay(Anim);
        Close();
    }

    private void SetVisual(double opacity, double scale)
    {
        Opacity = opacity;
        RenderTransform = TransformOperations.Parse($"scale({scale})");
    }

    private void OnBgPressed(object? sender, PointerPressedEventArgs e)
    {
        for (var v = e.Source as Visual; v is not null; v = v.GetVisualParent())
            if (v is Button) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _press = e;
        _pressPos = e.GetPosition(this);
    }
    private void OnBgMoved(object? sender, PointerEventArgs e)
    {
        if (_press is null || _dragging) return;
        if (Point.Distance(e.GetPosition(this), _pressPos) < 4) return;
        _dragging = true;
        var press = _press;
        _press = null;
        _ = DragAsync(press);
    }
    private void OnBgReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_press is null) return;
        _press = null;
        if (DataContext is ToashotViewModel vm) vm.SaveCommand.Execute(null);
    }
    private async Task DragAsync(PointerPressedEventArgs press)
    {
        try
        {
            if (DataContext is not ToashotViewModel vm) return;
            if (vm.FilePath is not { } file || !File.Exists(file)) return;
            if (StorageProvider?.TryGetFileFromPathAsync(new Uri(file)) is not { } task) return;
            if (await task is not { } item) return;
            var data = new DataTransfer();
            data.Add(DataTransferItem.CreateFile(item));
            vm.Msg = "拖到目标处放下：默认移动，按住 Ctrl 复制";
            var effect = await DragDrop.DoDragDropAsync(press, data, DragDropEffects.Move | DragDropEffects.Copy);
            vm.Msg = effect switch
            {
                DragDropEffects.Move => "已移动",
                DragDropEffects.Copy => "已复制",
                _ => "未放置"
            };
            if (effect == DragDropEffects.Move) await CloseAsync();
        }
        catch (Exception ex) { Console.WriteLine($"拖拽失败: {ex}"); }
        finally { _dragging = false; if (!_closing) _timer.Start(); }
    }

    private const int GwlExstyle = -20;
    private const int WsExToolwindow = 0x80;
    private const int WsExAppwindow = 0x40000;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;
    private void HideFromAltTab()
    {
        if (!OperatingSystem.IsWindows()) return;
        if (TryGetPlatformHandle() is not { Handle: var h } || h == IntPtr.Zero) return;
        var ex = GetWindowLongPtr(h, GwlExstyle);
        SetWindowLongPtr(h, GwlExstyle, (ex | WsExToolwindow) & ~WsExAppwindow);
        SetWindowPos(h, IntPtr.Zero, 0, 0, 0, 0,
            SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
    }
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

}
