using System.IO;
using System.Threading.Tasks;
using System;
using System.Threading;
using Avalonia.Threading;
using Shutter;
using Toashot.Avalonia.Common;
using Shutter.Models;
using Shutter.Enums;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using static Toashot.Avalonia.Common.AppConfig.ToashotSettings;
using Avalonia;

namespace Toashot.Modules.Toashot;

public static class ToashotSvc
{
    private static ToashotWindow? _toast;
    private static readonly SemaphoreSlim Gate = new(3, 3);

    private static string? _boundShot;
    private static string? _boundFast;
    private static void Rebind()
    {
        var t = AppConfiger.Config.Toashot;
        Listener.Instance.Unbind(_boundShot);
        Listener.Instance.Unbind(_boundFast);
        if(!t.Enabled) return;
        _boundShot = t.Shortcut;
        _boundFast = t.FastShot;
        Listener.Instance.Bind(_boundShot, Shot);
        Listener.Instance.Bind(_boundFast, FastShot);
    }
    private static void Shot()
        => Dispatcher.UIThread.Post(() => _ = ShotAsync());
    private static void FastShot() => _ = FastShotAsync();

    private static async Task<string> CaptureToFileAsync(string dir, ScreenshotOptions? options = null)
    {
        var bytes = await Task.Run(() => new ShutterService().TakeScreenshot(options));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        await File.WriteAllBytesAsync(path, bytes);
        return path;
    }
    private static Screen ScreenAtCursor(Screens screens)
        => screens.ScreenFromPoint(Listener.Instance.LastMouse)
            ?? screens.Primary
            ?? throw new InvalidOperationException("取不到主屏信息");

    private static async Task ShotAsync()
    {
        await Gate.WaitAsync();
        try
        {
            var win = CreateToast(out var vm);
            var screen = ScreenAtCursor(win.Screens ?? throw new InvalidOperationException("取不到屏幕信息"));
            var b = screen.Bounds;
            await Dispatcher.UIThread.InvokeAsync(() => _toast?.Close());
            var path = await CaptureToFileAsync(Path.GetTempPath(), RegionOptions(b));
            Dispatcher.UIThread.Post(() =>
            {
                vm.Image = new Bitmap(path);
                Place(win, screen);
                ShowToast(win);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"截图失败: {ex}");
            // TODO: 失败时给用户一个看得见的提示
        }
        finally
        {
            Gate.Release();
        }
    }
    private static async Task FastShotAsync()
    {
        await Gate.WaitAsync();
        try
        {
            var screens = CreateToast(out _).Screens ?? throw new InvalidOperationException("取不到屏幕信息");
            var b = ScreenAtCursor(screens).Bounds;
            await CaptureToFileAsync(AppConfiger.Config.Toashot.Path, RegionOptions(b));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"快速截图失败: {ex}");
            // TODO: 失败时给用户一个看得见的提示
        }
        finally
        {
            Gate.Release();
        }
    }

    private static ScreenshotOptions RegionOptions(PixelRect b) => new()
    {
        Target = CaptureTarget.Region,
        Region = new Rectangle { X = b.X, Y = b.Y, Width = b.Width, Height = b.Height },
    };

    private static ToashotWindow CreateToast(out ToashotViewModel vm)
    {
        vm = new ToashotViewModel(null, null);
        var win = new ToashotWindow(vm);
        win.Closed += (_, _) => _toast = null;
        return win;
    }
    private static void ShowToast(ToashotWindow win)
    {
        if (_toast?.DataContext is ToashotViewModel old) old.Image?.Dispose();
        _toast?.Close();
        _toast = win;
        win.Show();
    }

    private static void Place(Window win, Screen screen)
    {
        var t = AppConfiger.Config.Toashot;
        var b = screen.Bounds;
        var m = t.Margin * screen.Scaling;
        var w = win.Width * screen.Scaling;
        var h = win.Height * screen.Scaling;
        var right = t.Placement is ShotPlacement.TR or ShotPlacement.BR;
        var bottom = t.Placement is ShotPlacement.BL or ShotPlacement.BR;
        win.Position = new PixelPoint(
            (int)(right ? b.Right - w - m : b.X + m),
            (int)(bottom ? b.Bottom - h - m : b.Y + m));
    }

    public static void Start()
    {
        Listener.Instance.Start();
        Rebind();
        AppConfiger.Config.Toashot.PropertyChanged += (_, _) => Rebind();
    }
}