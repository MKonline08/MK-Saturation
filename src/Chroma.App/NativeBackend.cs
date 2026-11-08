using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
namespace MkSaturation;

public sealed class NativeBackend : IColorBackend, IDisposable
{
    private bool initialized;
    [StructLayout(LayoutKind.Sequential)]
    private struct Matrix { [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)] public float[] Values; }
    [DllImport("Magnification.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool MagInitialize();
    [DllImport("Magnification.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool MagUninitialize();
    [DllImport("Magnification.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool MagGetFullscreenColorEffect(out Matrix matrix);
    [DllImport("Magnification.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool MagSetFullscreenColorEffect(ref Matrix matrix);
    public NativeBackend()
    {
        if (RuntimeInformation.ProcessArchitecture != Architecture.Arm64 || RuntimeInformation.OSArchitecture != Architecture.Arm64)
            throw new PlatformNotSupportedException("Use the native Windows ARM64 build on Windows ARM64.");
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) throw new PlatformNotSupportedException("Windows 11 or later is required.");
        Marshal.SetLastPInvokeError(0);
        if (!MagInitialize()) throw Error("MagInitialize");
        initialized = true;
    }
    private static Exception Error(string operation)
    {
        int code = Marshal.GetLastPInvokeError();
        return new InvalidOperationException($"{operation} failed. Win32 error {code}" + (code == 0 ? " (Windows supplied no extended error)." : $": {new Win32Exception(code).Message}"));
    }
    public float[] Read()
    {
        ObjectDisposedException.ThrowIf(!initialized, this);
        Marshal.SetLastPInvokeError(0);
        if (!MagGetFullscreenColorEffect(out var m)) throw Error("MagGetFullscreenColorEffect");
        return m.Values;
    }
    public void Write(float[] values)
    {
        ObjectDisposedException.ThrowIf(!initialized, this);
        if (values.Length != 25) throw new ArgumentException("Expected a 5×5 matrix.");
        var m = new Matrix { Values = values };
        Marshal.SetLastPInvokeError(0);
        if (!MagSetFullscreenColorEffect(ref m)) throw Error("MagSetFullscreenColorEffect");
    }
    public void Dispose() { if (initialized) { MagUninitialize(); initialized = false; } }
}
