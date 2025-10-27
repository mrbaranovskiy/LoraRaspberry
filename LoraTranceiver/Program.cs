// See https://aka.ms/new-console-template for more information

using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.Spi;
using LoraLib;

var driver = new RaspberryPi3Driver();
var controller = new GpioController(driver);
var spiDevice = SpiDevice.Create(new SpiConnectionSettings(0,0));


var reset = controller.OpenPin(21);
var cs = controller.OpenPin(10);

var lora = new Lora(spiDevice,reset,cs);

 if (lora.Initialize(430_000_000) == 1)
 {
     byte[] buff = [32, 34];
     
     while (true)
     {
         Thread.Sleep(1000);
         lora.Write(buff, 2);
     }
 }
 else
 {
     Console.WriteLine("Cannot init chip");
 }

