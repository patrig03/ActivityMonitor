using System.Runtime.InteropServices;

namespace Backend.Interventions;

public static class NotifierBridge
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
        var o = NotifierBridge.notifier_notification(message, buttons, buttons.Length);
        return o == -1 ? string.Empty : buttons[o];
    }
    
    public static void TypingLock(string message, ulong windowId, string word) 
        => NotifierBridge.notifier_typing_lock(message, windowId, word);
    
    public static void TimedLock(string message, ulong windowId, int seconds) 
        => NotifierBridge.notifier_timed_lock(message, windowId, seconds);
}
