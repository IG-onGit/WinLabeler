using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WinLabeler;

internal static class VirtualDesktops
{
    // Documented Windows COM interface (shobjidl_core.h).
    [ComImport]
    [Guid("a5cd92ff-29be-454c-8d04-d82879fb3f1b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IVirtualDesktopManager
    {
        [PreserveSig]
        int IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow, [MarshalAs(UnmanagedType.Bool)] out bool onCurrentDesktop);

        [PreserveSig]
        int GetWindowDesktopId(IntPtr topLevelWindow, out Guid desktopId);

        [PreserveSig]
        int MoveWindowToDesktop(IntPtr topLevelWindow, [MarshalAs(UnmanagedType.LPStruct)] Guid desktopId);
    }

    private static readonly IVirtualDesktopManager? Manager = CreateManager();

    private static IVirtualDesktopManager? CreateManager()
    {
        try
        {
            var type = Type.GetTypeFromCLSID(new Guid("aa509086-5ca9-4c25-8f95-589d3c07b48a"));
            return type == null ? null : (IVirtualDesktopManager?)Activator.CreateInstance(type);
        }
        catch { return null; }
    }

    /// <summary>
    /// Returns the IDs of all virtual desktops in order, or null if they can't be read
    /// (e.g. only one desktop has ever existed in this profile).
    /// </summary>
    public static List<Guid>? GetDesktopIds()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops");
            if (key?.GetValue("VirtualDesktopIDs") is not byte[] bytes || bytes.Length < 16)
                return null;

            var list = new List<Guid>();
            for (int i = 0; i + 16 <= bytes.Length; i += 16)
                list.Add(new Guid(new ReadOnlySpan<byte>(bytes, i, 16)));
            return list;
        }
        catch { return null; }
    }

    /// <summary>The ID of the desktop currently being viewed, or null if it can't be read.</summary>
    public static Guid? GetCurrentDesktopId()
    {
        try
        {
            const string Explorer = @"Software\Microsoft\Windows\CurrentVersion\Explorer\";
            int session = System.Diagnostics.Process.GetCurrentProcess().SessionId;
            foreach (var path in new[]
            {
                $@"{Explorer}SessionInfo\{session}\VirtualDesktops",
                $@"{Explorer}VirtualDesktops",
            })
            {
                using var key = Registry.CurrentUser.OpenSubKey(path);
                if (key?.GetValue("CurrentVirtualDesktop") is byte[] bytes && bytes.Length >= 16)
                    return new Guid(new ReadOnlySpan<byte>(bytes, 0, 16));
            }
        }
        catch { }
        return null;
    }

    /// <summary>The desktop the window currently lives on, or null if unknown.</summary>
    public static Guid? GetWindowDesktop(IntPtr hwnd)
    {
        try
        {
            if (Manager != null && Manager.GetWindowDesktopId(hwnd, out var id) == 0)
                return id;
        }
        catch { }
        return null;
    }

    /// <summary>True/false if Windows says whether the window is on the desktop being viewed; null if unknown.</summary>
    public static bool? IsOnCurrentDesktop(IntPtr hwnd)
    {
        try
        {
            if (Manager != null && Manager.IsWindowOnCurrentVirtualDesktop(hwnd, out bool on) == 0)
                return on;
        }
        catch { }
        return null;
    }

    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumProc proc, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    private delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);

    private static Guid? DesktopIfCurrent(IntPtr hwnd)
    {
        if (Manager == null) return null;
        try
        {
            if (Manager.IsWindowOnCurrentVirtualDesktop(hwnd, out bool on) == 0 && on
                && Manager.GetWindowDesktopId(hwnd, out var id) == 0 && id != Guid.Empty)
                return id;
        }
        catch { }
        return null;
    }

    /// <summary>
    /// The desktop being viewed, taken from another program's window that Windows says is on it.
    /// The registry value can be stale (right after logon it names the previous session's desktop),
    /// and our own tool window is never assigned a desktop, so neither can be trusted at startup.
    /// Returns null when no ordinary window is on the current desktop.
    /// </summary>
    public static Guid? GetCurrentDesktopIdFromWindows(IntPtr exclude)
    {
        var fg = GetForegroundWindow();
        if (fg != IntPtr.Zero && fg != exclude && DesktopIfCurrent(fg) is Guid f) return f;

        Guid? found = null;
        EnumWindows((h, _) =>
        {
            if (h == exclude || !IsWindowVisible(h) || IsIconic(h)) return true;
            if (DesktopIfCurrent(h) is Guid d) { found = d; return false; }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    public static bool MoveToDesktop(IntPtr hwnd, Guid desktopId)
    {
        try { return Manager != null && Manager.MoveWindowToDesktop(hwnd, desktopId) == 0; }
        catch { return false; }
    }

    /// <summary>Moves the window to the desktop (if it isn't there) and verifies it landed.</summary>
    public static bool EnsureOnDesktop(IntPtr hwnd, Guid desktopId)
    {
        if (GetWindowDesktop(hwnd) == desktopId) return true;
        MoveToDesktop(hwnd, desktopId);
        return GetWindowDesktop(hwnd) == desktopId;
    }
}
