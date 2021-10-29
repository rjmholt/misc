using System;
using System.IO;
using System.Management.Automation;
using System.Runtime.InteropServices;

public class CloseHandler : IModuleAssemblyInitializer
{
    public void OnImport()
    {
        SetConsoleCtrlHandler(OnControlCode, add: true);
    }

    private static bool OnControlCode(CtrlType ctrlType)
    {
        switch (ctrlType)
        {
            case CtrlType.CTRL_CLOSE_EVENT:
            case CtrlType.CTRL_LOGOFF_EVENT:
            case CtrlType.CTRL_SHUTDOWN_EVENT:
                Console.Beep();
                break;
        }

        return false;
    }

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handlerRoutine, bool add);

    private delegate bool ConsoleCtrlDelegate(CtrlType ctrlType);

    private enum CtrlType : uint
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT = 1,
        CTRL_CLOSE_EVENT = 2,
        CTRL_LOGOFF_EVENT = 3,
        CTRL_SHUTDOWN_EVENT = 4,
    }
}