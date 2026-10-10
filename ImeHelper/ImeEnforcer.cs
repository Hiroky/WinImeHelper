using static ImeHelper.NativeMethods;

namespace ImeHelper;

/// <summary>
/// 前面ウィンドウの入力言語が日本語のとき、IME を常に ON (ひらがな) に保つ。
/// </summary>
internal sealed class ImeEnforcer : IDisposable
{
    private const int LANG_JAPANESE = 0x0411;
    private const int PollIntervalMs = 100;

    private readonly System.Windows.Forms.Timer _timer;
    private readonly WinEventDelegate _winEventProc; // GC されないよう保持
    private IntPtr _hook;
    private bool _enabled;

    public ImeEnforcer()
    {
        _timer = new System.Windows.Forms.Timer { Interval = PollIntervalMs };
        _timer.Tick += (_, _) => Enforce();
        _winEventProc = OnForegroundChanged;
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            _enabled = value;
            if (value)
            {
                _hook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
                    IntPtr.Zero, _winEventProc, 0, 0,
                    WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
                _timer.Start();
                Enforce();
            }
            else
            {
                _timer.Stop();
                if (_hook != IntPtr.Zero)
                {
                    UnhookWinEvent(_hook);
                    _hook = IntPtr.Zero;
                }
            }
        }
    }

    private void OnForegroundChanged(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) => Enforce();

    private static void Enforce()
    {
        try
        {
            IntPtr fg = GetForegroundWindow();
            if (fg == IntPtr.Zero) return;

            uint threadId = GetWindowThreadProcessId(fg, out _);
            if (threadId == 0) return;

            long hkl = GetKeyboardLayout(threadId).ToInt64();
            if ((hkl & 0xFFFF) != LANG_JAPANESE) return;

            // フォーカスを持つ子ウィンドウがあればそちらを優先
            IntPtr target = fg;
            var gti = new GUITHREADINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<GUITHREADINFO>() };
            if (GetGUIThreadInfo(threadId, ref gti) && gti.hwndFocus != IntPtr.Zero)
                target = gti.hwndFocus;

            IntPtr imeWnd = ImmGetDefaultIMEWnd(target);
            if (imeWnd == IntPtr.Zero) return;

            if (SendMessageTimeout(imeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETOPENSTATUS, IntPtr.Zero,
                    SMTO_ABORTIFHUNG, 50, out IntPtr open) == IntPtr.Zero)
                return;

            if (open == IntPtr.Zero)
            {
                SendMessageTimeout(imeWnd, WM_IME_CONTROL, (IntPtr)IMC_SETOPENSTATUS, (IntPtr)1,
                    SMTO_ABORTIFHUNG, 50, out _);
            }
        }
        catch
        {
            // 常駐ツールなので落とさない
        }
    }

    public void Dispose()
    {
        Enabled = false;
        _timer.Dispose();
    }
}
