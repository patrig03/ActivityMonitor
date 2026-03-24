using System.Diagnostics;
using System.Runtime.InteropServices;
using Backend.DataCollector.Models;

namespace Backend.DataCollector.Application;

public class WindowsAppCollector : IApplicationDataCollector
{
    public ApplicationRecord? GetActive()
    {
        var windowHandle = GetForegroundWindow();
        if (windowHandle == IntPtr.Zero || windowHandle == GetShellWindow() || !IsWindowVisible(windowHandle))
        {
            return null;
        }

        if (IsWindowCloaked(windowHandle))
        {
            return null;
        }

        _ = GetWindowThreadProcessId(windowHandle, out var processId);
        if (processId == 0)
        {
            return null;
        }

        var title = GetWindowText(windowHandle);
        var className = GetClassName(windowHandle);
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(className))
        {
            return null;
        }

        if (!GetWindowRect(windowHandle, out var rect))
        {
            return null;
        }

        return new ApplicationRecord
        {
            Id = null,
            CategoryId = null,
            ProcessName = GetProcessName((int)processId),
            WindowName = title,
            ClassName = className,
            PositionX = rect.Left,
            PositionY = rect.Top,
            Width = rect.Right - rect.Left,
            Height = rect.Bottom - rect.Top,
            WindowId = windowHandle.ToInt64()
        };
    }

    private static string? GetProcessName(int processId)
    {
        try
        {
            return Process.GetProcessById(processId).ProcessName;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetWindowText(IntPtr windowHandle)
    {
        var length = GetWindowTextLengthW(windowHandle);
        if (length <= 0)
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder(length + 1);
        _ = GetWindowTextW(windowHandle, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string? GetClassName(IntPtr windowHandle)
    {
        var builder = new System.Text.StringBuilder(256);
        var copied = GetClassNameW(windowHandle, builder, builder.Capacity);
        return copied > 0 ? builder.ToString() : null;
    }

    private static bool IsWindowCloaked(IntPtr windowHandle)
    {
        if (DwmGetWindowAttribute(windowHandle, DwmWindowAttributeCloaked, out var cloaked, sizeof(int)) != 0)
        {
            return false;
        }

        return cloaked != 0;
    }

    private const int DwmWindowAttributeCloaked = 14;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextW(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLengthW(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassNameW(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
