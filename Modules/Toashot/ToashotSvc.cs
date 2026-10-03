using System.IO;
using System.Threading.Tasks;
using System;
using System.Threading;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Shutter;
using Toashot.Avalonia.Common;

namespace Toashot.Modules.Toashot;

public static class ToashotSvc
{
    private static readonly SemaphoreSlim Gate = new(3, 3);
    private static readonly ShutterService Shutter = new();
    private static async Task<Bitmap> CaptureAsync()
        => new(new MemoryStream(await Task.Run(() => Shutter.TakeScreenshot())));

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

    private static async Task ShotAsync()
    {
        // TODO:
    }
    private static async Task FastShotAsync()
    {
        await Gate.WaitAsync();
        try
        {
            var bytes = await Task.Run(() => new ShutterService().TakeScreenshot());
            var dir = AppConfiger.Config.Toashot.Path;
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            await File.WriteAllBytesAsync(path, bytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"快速截图失败: {ex}");
            // TODO: 给用户看的提示
        }
        finally
        {
            Gate.Release();
        }
    }

    public static void Start()
    {
        Listener.Instance.Start();
        Rebind();
        AppConfiger.Config.Toashot.PropertyChanged += (_, _) => Rebind();
    }
}