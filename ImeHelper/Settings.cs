using Microsoft.Win32;

namespace ImeHelper;

/// <summary>
/// ユーザー設定 (HKCU\Software\ImeHelper に保存)。
/// </summary>
internal static class Settings
{
    private const string KeyPath = @"Software\ImeHelper";

    public const int HighFrequencyIntervalMs = 100;
    public const int NormalIntervalMs = ImeEnforcer.DefaultIntervalMs;

    public static int IntervalMs
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            return key?.GetValue(nameof(IntervalMs)) is int v && v > 0 ? v : NormalIntervalMs;
        }
        set
        {
            using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
            key.SetValue(nameof(IntervalMs), value, RegistryValueKind.DWord);
        }
    }
}
