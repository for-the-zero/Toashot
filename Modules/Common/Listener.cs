using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input;
using SharpHook;
using SharpHook.Data;

namespace Toashot.Avalonia.Common;

public sealed class Listener
{
    public static Listener Instance { get; } = new();

    private readonly List<(Key Key, KeyModifiers KeyModifiers, Action Callback)> _bindings = new();
    private readonly EventLoopGlobalHook _hook = new(useBackgroundThreadForEventLoop: true);
    private KeyModifiers _pressedModifiers;
    private readonly HashSet<Key> _downKeys = new();

    public event Action<Key, KeyModifiers>? AnyKeyPressed;
    public event Action<Key>? AnyKeyReleased;
    public event Action<SharpHook.Data.MouseButton>? AnyMousePressed;
    public event Action<int>? AnyWheel;

    private Listener()
    {
        _hook.KeyPressed += OnKeyPressed;
        _hook.KeyReleased += OnKeyReleased;
        _hook.MousePressed += (_, e) => AnyMousePressed?.Invoke(e.Data.Button);
        _hook.MouseWheel += (_, e) => AnyWheel?.Invoke(e.Data.Rotation);
        // TODO: 鼠标移动/拖拽/按键抬起(KeyTyped)/点击，用到了再挂
    }

    private Task? _running;

    /// <summary>开始监听，重复调用无害。后台线程跑，不阻塞调用方。</summary>
    public void Start() => _running ??= _hook.RunAsync(GlobalHookType.All, useBackgroundThread: true);

    /// <summary>钩子起来了吗。没起来一般是被系统权限挡住了。</summary>
    public bool IsRunning => _running is { IsCompleted: false };

    // TODO: 启动失败（权限不足）时给用户一个提示，现在只是静默不监听
    public void Stop() => _hook.Stop();

    public void Bind(string? gesture, Action callback)
    {
        if (!TryParse(gesture, out var key, out var modifiers)) return;
        lock (_bindings) _bindings.Add((key, modifiers, callback));
    }
    public bool Unbind(string? gesture)
    {
        if (!TryParse(gesture, out var key, out var modifiers)) return false;
        lock (_bindings)
        {
            var index = _bindings.FindIndex(b => b.Key == key && b.KeyModifiers == modifiers);
            if (index < 0) return false;
            _bindings.RemoveAt(index);
            return true;
        }
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        var key = ToAvaloniaKey(e.Data.KeyCode);
        var modifiers = TrackModifiers(e.Data.KeyCode, pressed: true);
        AnyKeyPressed?.Invoke(key, modifiers);
        if (key == Key.None) return;
        if (!_downKeys.Add(key)) return;
        (Key Key, KeyModifiers KeyModifiers, Action Callback)[] snapshot;
        lock (_bindings) snapshot = _bindings.ToArray();
        foreach (var binding in snapshot)
            if (binding.Key == key && binding.KeyModifiers == modifiers)
            {
                binding.Callback();
                return;
            }
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        var key = ToAvaloniaKey(e.Data.KeyCode);
        _downKeys.Remove(key);
        TrackModifiers(e.Data.KeyCode, pressed: false);
        AnyKeyReleased?.Invoke(key);
    }

    private KeyModifiers TrackModifiers(KeyCode code, bool pressed)
    {
        var flag = code switch
        {
            KeyCode.VcLeftControl or KeyCode.VcRightControl => KeyModifiers.Control,
            KeyCode.VcLeftAlt or KeyCode.VcRightAlt => KeyModifiers.Alt,
            KeyCode.VcLeftShift or KeyCode.VcRightShift => KeyModifiers.Shift,
            KeyCode.VcLeftMeta or KeyCode.VcRightMeta => KeyModifiers.Meta,
            _ => KeyModifiers.None,
        };
        if (flag == KeyModifiers.None) return _pressedModifiers;
        if (pressed) _pressedModifiers |= flag;
        else _pressedModifiers &= ~flag;
        return _pressedModifiers;
    }

    private static bool TryParse(string? gesture, out Key key, out KeyModifiers modifiers)
    {
        key = Key.None;
        modifiers = KeyModifiers.None;
        if (string.IsNullOrWhiteSpace(gesture)) return false;
        try
        {
            var parsed = KeyGesture.Parse(gesture);
            key = parsed.Key;
            modifiers = parsed.KeyModifiers;
            return key != Key.None;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static Key ToAvaloniaKey(KeyCode code)
    {
        var name = code.ToString();
        if (name.StartsWith("Vc", StringComparison.Ordinal))
            name = name[2..];
        if (Enum.TryParse<Key>(name, out var key)) return key;
        return code switch
        {
            KeyCode.Vc0 => Key.D0,
            KeyCode.Vc1 => Key.D1,
            KeyCode.Vc2 => Key.D2,
            KeyCode.Vc3 => Key.D3,
            KeyCode.Vc4 => Key.D4,
            KeyCode.Vc5 => Key.D5,
            KeyCode.Vc6 => Key.D6,
            KeyCode.Vc7 => Key.D7,
            KeyCode.Vc8 => Key.D8,
            KeyCode.Vc9 => Key.D9,
            KeyCode.VcMinus => Key.OemMinus,
            KeyCode.VcEquals => Key.OemPlus,
            KeyCode.VcBackspace => Key.Back,
            KeyCode.VcSemicolon => Key.OemSemicolon,
            KeyCode.VcQuote => Key.OemQuotes,
            KeyCode.VcBackQuote => Key.OemTilde,
            KeyCode.VcOpenBracket => Key.OemOpenBrackets,
            KeyCode.VcCloseBracket => Key.OemCloseBrackets,
            KeyCode.VcBackslash => Key.OemPipe,
            KeyCode.VcComma => Key.OemComma,
            KeyCode.VcPeriod => Key.OemPeriod,
            KeyCode.VcSlash => Key.OemQuestion,
            _ => Key.None,
        };
    }
}
