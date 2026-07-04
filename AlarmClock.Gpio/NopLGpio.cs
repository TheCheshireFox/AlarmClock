namespace AlarmClock.Gpio;

public class NopLGpio : ILGpio
{
    public NopLGpio() => Console.WriteLine("*** NOP LGpio ***");
    
    public int Open(int gpioDev) => 1;
    public int Close(int handle) => 1;
    public int TxPwm(int handle, int gpio, float pwmFrequency, float pwmDutyCycle, int pwmOffset, int pwmCycles) => 1;
    public int ClaimOutput(int handle, int lFlags, int line, int level) => 1;
    public int Write(int handle, int line, int level) => 1;
}