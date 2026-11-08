using System;
using System.Runtime.InteropServices;
using System.Text;
namespace MkSaturation;
public static class Diagnostics
{
    public static string Run(bool smoke, out int exitCode)
    {
        exitCode = 0;
        var report = new StringBuilder($"MK Saturation 1.0.0\nUTC: {DateTime.UtcNow:O}\nOS: {RuntimeInformation.OSDescription}\nProcess: {RuntimeInformation.ProcessArchitecture}\nOS architecture: {RuntimeInformation.OSArchitecture}\n");
        try
        {
            using var backend = new NativeBackend();
            var original = backend.Read();
            report.AppendLine("MagInitialize / MagGetFullscreenColorEffect: PASS");
            report.AppendLine("Initial matrix: " + string.Join(", ", original));
            if (smoke)
            {
                try
                {
                    backend.Write(original);
                    if (!ColorMatrix.Equal(original, backend.Read())) throw new InvalidOperationException("Readback mismatch.");
                    report.AppendLine("Write unchanged matrix / readback: PASS");
                }
                finally { backend.Write(original); }
            }
        }
        catch (Exception ex) { report.AppendLine("FAIL: " + ex); exitCode = 1; }
        report.AppendLine("No visual coverage claim. Test SDR desktop, HDR, video and games separately.\nNo GPU serial numbers, account names or file paths are collected by this report.");
        return report.ToString();
    }
}
