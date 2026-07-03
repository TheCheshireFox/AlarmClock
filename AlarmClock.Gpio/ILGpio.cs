namespace AlarmClock.Gpio;

public interface ILGpio
{
    int Open(int gpioDev);
    int Close(int handle);
    int TxPwm(int handle, int gpio, float pwmFrequency, float pwmDutyCycle, int pwmOffset, int pwmCycles);
    int ClaimOutput(int handle, int lFlags, int line, int level);
    int Write(int handle, int line, int level);
}