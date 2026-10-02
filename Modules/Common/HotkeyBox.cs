using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace Toashot.Avalonia.Common;

public class HotkeyBox : TextBox
{
    protected override Type StyleKeyOverride => typeof(TextBox);

    public HotkeyBox()
    {
        IsReadOnly = true;
        GotFocus += (_, _) =>
        {
            _backup = Text;
            _recording = true;
            Listener.Instance.Start();
            Listener.Instance.AnyKeyPressed += OnGlobalKeyPressed;
        };
        LostFocus += (_, _) =>
        {
            _recording = false;
            _backup = null;
            Listener.Instance.AnyKeyPressed -= OnGlobalKeyPressed;
        };
    }

    private string? _backup;
    private bool _recording;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Text = null;
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Text = _backup;
            e.Handled = true;
            return;
        }
        if (e.Key is Key.Tab or Key.Enter or Key.Back or Key.None) return;
        e.Handled = true;
    }
    private void OnGlobalKeyPressed(Key key, KeyModifiers modifiers)
    {
        if (key == Key.None || key == Key.Escape || key == Key.Tab || key == Key.Enter || key == Key.Back) return;
        if (IsModifierKey(key)) return;
        Dispatcher.UIThread.Post(() =>
        {
            if (!_recording) return;
            Text = Serialize(new KeyGesture(key, modifiers));
        });
    }
    private static bool IsModifierKey(Key key)
        => key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin;

    public static string Serialize(KeyGesture gesture)
    {
        var parts = new List<string>(5);
        if (gesture.KeyModifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (gesture.KeyModifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        if (gesture.KeyModifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        if (gesture.KeyModifiers.HasFlag(KeyModifiers.Meta)) parts.Add("Win");
        parts.Add(gesture.Key.ToString());
        return string.Join("+", parts);
    }
}
