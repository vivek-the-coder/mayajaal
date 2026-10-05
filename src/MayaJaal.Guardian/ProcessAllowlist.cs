using MayaJaal.Shared.Models;

namespace MayaJaal.Guardian;

/// <summary>
/// Lightweight allowlist to reduce noise from trusted Windows processes.
/// Honey events always bypass the allowlist.
/// </summary>
internal static class ProcessAllowlist
{
    private static readonly HashSet<string> TrustedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer.exe", "svchost.exe", "services.exe", "lsass.exe",
        "winlogon.exe", "taskhostw.exe", "taskhost.exe", "dwm.exe", "csrss.exe",
        "runtimebroker.exe", "ctfmon.exe", "sihost.exe", "searchhost.exe",
        "startmenuexperiencehost.exe", "shellexperiencehost.exe"
    };

    private static readonly string[] TrustedPathPrefixes =
    [
        @"C:\Windows\System32\",
        @"C:\Windows\SysWOW64\",
        @"C:\Windows\SystemApps\"
    ];

    public static bool IsAllowed(SecurityEvent evt)
    {
        if (evt.IsHoney || evt.Type is EventType.HONEY_ACCESS or EventType.HONEY_MODIFY)
            return false;

        var name = evt.Process?.ProcessName;
        if (!string.IsNullOrWhiteSpace(name) && TrustedNames.Contains(Path.GetFileName(name)))
            return true;

        var path = evt.Process?.ProcessPath ?? evt.Path;
        if (!string.IsNullOrWhiteSpace(path) &&
            TrustedPathPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }
}
