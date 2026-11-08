using System;
using Microsoft.Win32;
namespace MkSaturation;
public static class StartupRegistration
{
    private const string Key = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(Key);
        return string.Equals(key?.GetValue("MK Saturation") as string, Command, StringComparison.OrdinalIgnoreCase);
    }
    private static string Command => "\"" + Environment.ProcessPath + "\" --startup";
    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(Key, true);
        if (enabled) key.SetValue("MK Saturation", Command); else key.DeleteValue("MK Saturation", false);
    }
}
