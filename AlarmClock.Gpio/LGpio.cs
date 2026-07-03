namespace AlarmClock.Gpio;

public class LGpio : ILGpio
{
    public int Open(int gpioDev)
        => LgpioLib.GpiochipOpen(gpioDev);

    public int Close(int handle)
        => LgpioLib.GpiochipClose(handle);

    public int TxPwm(int handle, int gpio, float pwmFrequency, float pwmDutyCycle, int pwmOffset, int pwmCycles)
        => LgpioLib.TxPwm(handle, gpio, pwmFrequency, pwmDutyCycle, pwmOffset, pwmCycles);

    public int ClaimOutput(int handle, int lFlags, int line, int level)
        => LgpioLib.GpioClaimOutput(handle, lFlags, line, level);

    public int Write(int handle, int line, int level)
        => LgpioLib.GpioWrite(handle, line, level);
}