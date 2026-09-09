using System.Runtime.InteropServices;

namespace NoitaTrainer;

internal static class Program
{
    private const string SingleInstanceMutexName = "CodexNoitaTrainer_SingleInstance";
    private const string ShowWindowMessageName = "CodexNoitaTrainer_ShowWindow";

    [STAThread]
    private static void Main()
    {
        using var singleInstance = new Mutex(true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            // 已有一个实例在运行：通知它把窗口弹到前台，然后本实例直接退出
            var showMessage = User32.RegisterWindowMessage(ShowWindowMessageName);
            User32.PostMessage(User32.HwndBroadcast, showMessage, IntPtr.Zero, IntPtr.Zero);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal static class User32
{
    public const int HwndBroadcast = 0xFFFF;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int RegisterWindowMessage(string message);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
}
