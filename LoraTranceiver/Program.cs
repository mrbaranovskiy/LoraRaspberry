// See https://aka.ms/new-console-template for more information

using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.Spi;
using System.Text;
using LoraLib;

var driver = new RaspberryPi3Driver();
var controller = new GpioController(driver);
var spiDevice = SpiDevice.Create(new SpiConnectionSettings(0, 0) {ClockFrequency = 100000});


var reset = controller.OpenPin(5);
var cs = controller.OpenPin(8);

var lora = new Lora(spiDevice, reset, cs);

uint freq = 433_000_000;
string payload = "";
byte power = 7;

Console.WriteLine(args.Length);

if (args.Length > 0)
{
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i].Trim() == "--f")
            freq = uint.Parse(args[i + 1]);

        if (args[i].Trim() == "--p")
            payload = args[i + 1];

        if (args[i].Trim() == "--power")
        {
            power = byte.Parse(args[i + 1]);
        }

    } 
}

if (power > 17)
    power = 17;

if (string.IsNullOrEmpty(payload))
{
    Console.WriteLine("No payload");
   Environment.Exit(-1);
}

if (freq is < 410_000_000 or > 510_000_000)
{
    Console.WriteLine("Frequency is out of range");
    Environment.Exit(-1);
}

Console.WriteLine($"F is {freq}");

if (lora.Initialize(freq) == 1)
{
    byte[] buff = Encoding.UTF8.GetBytes(payload);

    {
        try
        {
           
            Thread.Sleep(1000);
            
            lora.BeginPacket(0);
            var sent = lora.Write(buff, (byte)buff.Length);
            lora.EndPacket();

            while (sent < buff.Length)
            {
                lora.BeginPacket(0);
                var asSpan = buff.AsSpan(sent - 1).ToArray();
                var writen = lora.Write(asSpan, (byte)(buff.Length - sent));
                Console.WriteLine(writen.ToString());
                Thread.Sleep(10);
                sent += writen;
                lora.EndPacket();
            }
            
            reset.Write(PinValue.Low);
            Thread.Sleep(100);
            reset.Write(PinValue.High);
            lora.EndPacket();
            Console.WriteLine("end");
            lora.Idle();
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
   
}
else
{
    Console.WriteLine("Cannot init chip");
}


