using System.Runtime.InteropServices;

namespace AlarmClock.Gpio;

internal static class LgpioLib
{
    private const string Lib = "lgpio";

    [DllImport(Lib, EntryPoint = "lgGpiochipOpen", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GpiochipOpen(int gpioDev);

    [DllImport(Lib, EntryPoint = "lgGpiochipClose", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GpiochipClose(int handle);

    [DllImport(Lib, EntryPoint = "lgTxPwm", CallingConvention = CallingConvention.Cdecl)]
    public static extern int TxPwm(int handle, int gpio, float pwmFrequency, float pwmDutyCycle, int pwmOffset, int pwmCycles);

    [DllImport(Lib, EntryPoint = "lgGpioClaimOutput", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GpioClaimOutput(int handle, int lFlags, int line, int level);

    [DllImport(Lib, EntryPoint = "lgGpioWrite", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GpioWrite(int handle, int line, int level);
}
