using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Backend.Interventions;

internal static class LinuxNotifierBridge
{
    [DllImport("Notifier", CallingConvention = CallingConvention.Cdecl)]
    public static extern int notifier_notification(
        string message,
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)]
        string[] buttons,
        int buttonCount
    );

    [DllImport("Notifier", CallingConvention = CallingConvention.Cdecl)]
    public static extern void notifier_typing_lock(string message, ulong windowId, string word);
    
    [DllImport("Notifier", CallingConvention = CallingConvention.Cdecl)]
    public static extern void notifier_timed_lock(string message, ulong windowId, int seconds);
    
}

public static class Notifier
{
    public static string Notification(string message, string[] buttons)
    {
        if (OperatingSystem.IsWindows())
        {
            return WindowsNotifier.Notification(message, buttons);
        }

        var index = LinuxNotifierBridge.notifier_notification(message, buttons, buttons.Length);
        return index is < 0 || index >= buttons.Length ? string.Empty : buttons[index];
    }
    
    public static void TypingLock(string message, long windowId, string word)
    {
        if (OperatingSystem.IsWindows())
        {
            WindowsNotifier.TypingLock(message, windowId, word);
            return;
        }

        LinuxNotifierBridge.notifier_typing_lock(message, unchecked((ulong)windowId), word);
    }
    
    public static void TimedLock(string message, long windowId, int seconds)
    {
        if (OperatingSystem.IsWindows())
        {
            WindowsNotifier.TimedLock(message, windowId, seconds);
            return;
        }

        LinuxNotifierBridge.notifier_timed_lock(message, unchecked((ulong)windowId), seconds);
    }
}

internal static class WindowsNotifier
{
    private const string WindowTitle = "Activity Monitor";
    private const uint MessageBoxOk = 0x00000000;
    private const uint MessageBoxYesNo = 0x00000004;
    private const uint MessageBoxIconWarning = 0x00000030;
    private const uint MessageBoxTopMost = 0x00040000;
    private const uint MessageBoxSetForeground = 0x00010000;
    private const int MessageBoxIdYes = 6;
    private const int MessageBoxIdNo = 7;

    public static string Notification(string message, string[] buttons)
    {
        if (buttons.Length == 0)
        {
            return string.Empty;
        }

        if (TryRunPowerShellScript(BuildNotificationScript(message, buttons), out var output) &&
            int.TryParse(output, out var index) &&
            index >= 0 &&
            index < buttons.Length)
        {
            return buttons[index];
        }

        return FallbackNotification(message, buttons);
    }

    public static void TypingLock(string message, long windowId, string word)
    {
        WithDisabledWindow(windowId, () =>
        {
            if (!TryRunPowerShellScript(BuildTypingLockScript(message, word), out _))
            {
                ShowMessageBoxW(
                    IntPtr.Zero,
                    $"{message}{Environment.NewLine}{Environment.NewLine}Unlock phrase: {word}",
                    WindowTitle,
                    MessageBoxOk | MessageBoxIconWarning | MessageBoxTopMost | MessageBoxSetForeground);
            }
        });
    }

    public static void TimedLock(string message, long windowId, int seconds)
    {
        WithDisabledWindow(windowId, () =>
        {
            if (!TryRunPowerShellScript(BuildTimedLockScript(message, seconds), out _))
            {
                Thread.Sleep(TimeSpan.FromSeconds(Math.Max(0, seconds)));
            }
        });
    }

    private static void WithDisabledWindow(long windowId, Action action)
    {
        var windowHandle = new IntPtr(windowId);
        var canDisable = windowHandle != IntPtr.Zero && IsWindow(windowHandle);
        var wasEnabled = canDisable && IsWindowEnabled(windowHandle);

        if (wasEnabled)
        {
            EnableWindow(windowHandle, false);
        }

        try
        {
            action();
        }
        finally
        {
            if (wasEnabled && IsWindow(windowHandle))
            {
                EnableWindow(windowHandle, true);
                SetForegroundWindow(windowHandle);
            }
        }
    }

    private static string FallbackNotification(string message, string[] buttons)
    {
        if (buttons.Length == 1)
        {
            ShowMessageBoxW(
                IntPtr.Zero,
                message,
                WindowTitle,
                MessageBoxOk | MessageBoxIconWarning | MessageBoxTopMost | MessageBoxSetForeground);
            return buttons[0];
        }

        if (buttons.Length >= 2)
        {
            var result = ShowMessageBoxW(
                IntPtr.Zero,
                message,
                WindowTitle,
                MessageBoxYesNo | MessageBoxIconWarning | MessageBoxTopMost | MessageBoxSetForeground);

            return result == MessageBoxIdNo ? buttons[1] : buttons[0];
        }

        return string.Empty;
    }

    private static string BuildNotificationScript(string message, IReadOnlyList<string> buttons)
    {
        var width = Math.Max(360, 40 + (buttons.Count * 130));
        var buttonWidth = 110;
        var totalButtonWidth = buttons.Count * buttonWidth + Math.Max(0, buttons.Count - 1) * 10;
        var startX = Math.Max(20, (width - totalButtonWidth) / 2);
        var buttonBuilders = new StringBuilder();

        for (var i = 0; i < buttons.Count; i++)
        {
            var x = startX + i * (buttonWidth + 10);
            buttonBuilders.AppendLine($"$button{i} = New-Object System.Windows.Forms.Button");
            buttonBuilders.AppendLine($"$button{i}.Text = {ToPowerShellString(buttons[i])}");
            buttonBuilders.AppendLine($"$button{i}.Size = New-Object System.Drawing.Size({buttonWidth}, 34)");
            buttonBuilders.AppendLine($"$button{i}.Location = New-Object System.Drawing.Point({x}, 108)");
            buttonBuilders.AppendLine($"$button{i}.Add_Click({{ $script:result = {i}; $form.Close() }})");
            buttonBuilders.AppendLine($"$form.Controls.Add($button{i})");
        }

        buttonBuilders.AppendLine("$form.AcceptButton = $button0");

        return $$"""
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()
$form = New-Object System.Windows.Forms.Form
$form.Text = {{ToPowerShellString(WindowTitle)}}
$form.StartPosition = 'CenterScreen'
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false
$form.MinimizeBox = $false
$form.ShowInTaskbar = $true
$form.TopMost = $true
$form.ClientSize = New-Object System.Drawing.Size({{width}}, 160)
$form.AutoScaleMode = 'Dpi'
$label = New-Object System.Windows.Forms.Label
$label.Text = {{ToPowerShellString(message)}}
$label.Location = New-Object System.Drawing.Point(20, 18)
$label.Size = New-Object System.Drawing.Size({{width - 40}}, 72)
$label.TextAlign = 'MiddleCenter'
$label.Font = New-Object System.Drawing.Font('Segoe UI', 10)
$form.Controls.Add($label)
$script:result = -1
{{buttonBuilders}}
[void]$form.ShowDialog()
Write-Output $script:result
""";
    }

    private static string BuildTypingLockScript(string message, string word)
    {
        return $$"""
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()
$form = New-Object System.Windows.Forms.Form
$form.Text = {{ToPowerShellString(WindowTitle)}}
$form.StartPosition = 'CenterScreen'
$form.FormBorderStyle = 'FixedDialog'
$form.ControlBox = $false
$form.MaximizeBox = $false
$form.MinimizeBox = $false
$form.ShowInTaskbar = $true
$form.TopMost = $true
$form.ClientSize = New-Object System.Drawing.Size(460, 220)
$form.AutoScaleMode = 'Dpi'
$label = New-Object System.Windows.Forms.Label
$label.Text = {{ToPowerShellString(message)}}
$label.Location = New-Object System.Drawing.Point(20, 20)
$label.Size = New-Object System.Drawing.Size(420, 44)
$label.TextAlign = 'MiddleCenter'
$label.Font = New-Object System.Drawing.Font('Segoe UI', 10)
$form.Controls.Add($label)
$hint = New-Object System.Windows.Forms.Label
$hint.Text = {{ToPowerShellString($"Type exactly: {word}")}}
$hint.Location = New-Object System.Drawing.Point(20, 74)
$hint.Size = New-Object System.Drawing.Size(420, 24)
$hint.TextAlign = 'MiddleCenter'
$form.Controls.Add($hint)
$textBox = New-Object System.Windows.Forms.TextBox
$textBox.Location = New-Object System.Drawing.Point(30, 116)
$textBox.Size = New-Object System.Drawing.Size(400, 30)
$textBox.Font = New-Object System.Drawing.Font('Segoe UI', 10)
$form.Controls.Add($textBox)
$button = New-Object System.Windows.Forms.Button
$button.Text = 'Unlock'
$button.Enabled = $false
$button.Location = New-Object System.Drawing.Point(170, 164)
$button.Size = New-Object System.Drawing.Size(120, 34)
$button.Add_Click({ if ($textBox.Text -eq {{ToPowerShellString(word)}}) { $form.Tag = 'unlock'; $form.Close() } })
$form.Controls.Add($button)
$textBox.Add_TextChanged({ $button.Enabled = ($textBox.Text -eq {{ToPowerShellString(word)}}) })
$form.Add_Shown({ $textBox.Focus() })
$form.Add_FormClosing({ if ($form.Tag -ne 'unlock') { $_.Cancel = $true } })
[void]$form.ShowDialog()
""";
    }

    private static string BuildTimedLockScript(string message, int seconds)
    {
        return $$"""
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()
$form = New-Object System.Windows.Forms.Form
$form.Text = {{ToPowerShellString(WindowTitle)}}
$form.StartPosition = 'CenterScreen'
$form.FormBorderStyle = 'FixedDialog'
$form.ControlBox = $false
$form.MaximizeBox = $false
$form.MinimizeBox = $false
$form.ShowInTaskbar = $true
$form.TopMost = $true
$form.ClientSize = New-Object System.Drawing.Size(420, 180)
$form.AutoScaleMode = 'Dpi'
$title = New-Object System.Windows.Forms.Label
$title.Text = {{ToPowerShellString(message)}}
$title.Location = New-Object System.Drawing.Point(20, 20)
$title.Size = New-Object System.Drawing.Size(380, 44)
$title.TextAlign = 'MiddleCenter'
$title.Font = New-Object System.Drawing.Font('Segoe UI', 10)
$form.Controls.Add($title)
$countdown = New-Object System.Windows.Forms.Label
$countdown.Location = New-Object System.Drawing.Point(20, 84)
$countdown.Size = New-Object System.Drawing.Size(380, 32)
$countdown.TextAlign = 'MiddleCenter'
$countdown.Font = New-Object System.Drawing.Font('Segoe UI', 18, [System.Drawing.FontStyle]::Bold)
$form.Controls.Add($countdown)
$script:remaining = {{Math.Max(0, seconds)}}
$timer = New-Object System.Windows.Forms.Timer
$timer.Interval = 1000
$timer.Add_Tick({
    $script:remaining--
    if ($script:remaining -le 0) {
        $timer.Stop()
        $form.Tag = 'done'
        $form.Close()
        return
    }

    $countdown.Text = "$($script:remaining) seconds remaining"
})
$form.Add_Shown({
    $countdown.Text = "$($script:remaining) seconds remaining"
    $timer.Start()
})
$form.Add_FormClosing({ if ($form.Tag -ne 'done' -and $script:remaining -gt 0) { $_.Cancel = $true } })
[void]$form.ShowDialog()
""";
    }

    private static string ToPowerShellString(string value) => $"'{value.Replace("'", "''")}'";

    private static bool TryRunPowerShellScript(string script, out string output)
    {
        var encodedScript = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

        foreach (var command in new[] { "powershell.exe", "pwsh.exe" })
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = $"-NoLogo -NoProfile -STA -ExecutionPolicy Bypass -EncodedCommand {encodedScript}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var standardOutput = process.StandardOutput.ReadToEnd();
                _ = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    output = standardOutput.Trim();
                    return true;
                }
            }
            catch
            {
            }
        }

        output = string.Empty;
        return false;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ShowMessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnableWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool enable);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
