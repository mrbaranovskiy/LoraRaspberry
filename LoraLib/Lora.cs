using System.Device.Gpio;
using System.Device.Spi;

namespace LoraLib;

public interface ILora
{
    byte Initialize(uint frequency);
    void SetTxPower(byte level);
    byte BeginPacket(byte implicitHeader);
    byte EndPacket();
    int ParsePacket(int size);
    sbyte PacketRssi();
    byte Write(byte[] buffer, byte size);
    void Receive(int size);
    sbyte Read();
    sbyte Available();
    sbyte Peek();
}

public class Lora(SpiDevice spi, GpioPin resetPin, GpioPin cs) : ILora
{
    private readonly SpiDevice _spi = spi ?? throw new ArgumentNullException(nameof(spi));
    private byte implicitHeaderMode = 0;
    private byte packetIndex = 0;
    private uint _frequency = 0;

    public byte Initialize(uint frequency)
    {
        resetPin.Write(PinValue.Low);
        Thread.Sleep(20); // bzzzz
        resetPin.Write(PinValue.High);
        Thread.Sleep(20); // bzzzz

        var version = ReadRegister(Regs.REG_VERSION);

        if (version != 0x12)
            return 0;
        Console.Write($"Version {version}");
        SetFrequency(frequency);
        WriteRegister(Regs.REG_FIFO_TX_BASE_ADDR, 0);
        WriteRegister(Regs.REG_FIFO_RX_BASE_ADDR, 0);
        WriteRegister(Regs.REG_LNA, (byte)(ReadRegister(Regs.REG_LNA) | 0x03));
        WriteRegister(Regs.REG_MODEM_CONFIG_3, 0x04);
        SetTxPower(7);
   
        Idle();
        
        _frequency = frequency;
        return 1;
    }

    /// <summary>
    public byte BeginPacket(byte implicitHeader)
    {
        Idle();

        if (implicitHeader != 0)
            ImplicitHeaderMode();
        else
            ExplicitHeaderMode();


        WriteRegister(Regs.REG_FIFO_ADDR_PTR, 0);
        WriteRegister(Regs.REG_PAYLOAD_LENGTH, 0);

        return 1;
    }
    public byte EndPacket()
    {
        WriteRegister(Regs.REG_OP_MODE, Regs.MODE_LONG_RANGE_MODE | Regs.MODE_TX);

        while ((ReadRegister(Regs.REG_IRQ_FLAGS) & Regs.IRQ_TX_DONE_MASK) == 0)
        {
            WriteRegister(Regs.REG_IRQ_FLAGS, Regs.IRQ_TX_DONE_MASK);
            Console.WriteLine("Wait end...");
        }

      
        return 1;
    }
    public int ParsePacket(int size)
    {
        int packetLength = 0;
        int irqFlags = ReadRegister(Regs.REG_IRQ_FLAGS);

        if (size > 0)
        {
            ImplicitHeaderMode();
            WriteRegister(Regs.REG_PAYLOAD_LENGTH, (byte)(size & 0xFF));
        }
        else
        {
            ExplicitHeaderMode();
        }

        WriteRegister(Regs.REG_IRQ_FLAGS, (byte)irqFlags);

        if ((irqFlags & Regs.IRQ_RX_DONE_MASK) != 0 &&
            (irqFlags & Regs.IRQ_PAYLOAD_CRC_ERROR_MASK) == 0)
        {
            packetIndex = 0;

            if (implicitHeaderMode > 0)
                packetLength = ReadRegister(Regs.REG_PAYLOAD_LENGTH);
            else
                packetLength = ReadRegister(Regs.REG_RX_NB_BYTES);

            WriteRegister(Regs.REG_FIFO_ADDR_PTR, ReadRegister(Regs.REG_FIFO_RX_CURRENT_ADDR));

            Idle();
        }
        else if (ReadRegister(Regs.REG_OP_MODE) != (Regs.MODE_LONG_RANGE_MODE | Regs.MODE_RX_SINGLE))
        {
            WriteRegister(Regs.REG_FIFO_ADDR_PTR, 0);
            WriteRegister(Regs.REG_OP_MODE, Regs.MODE_LONG_RANGE_MODE | Regs.MODE_RX_SINGLE);
        }

        return packetLength;
    }

    private void ImplicitHeaderMode()
    {
        implicitHeaderMode = 1;
        WriteRegister(Regs.REG_MODEM_CONFIG_1, ReadRegister(Regs.REG_MODEM_CONFIG_1 | 0x01));
    }

    private void ExplicitHeaderMode()
    {
        implicitHeaderMode = 0;
        WriteRegister(Regs.REG_MODEM_CONFIG_1, ReadRegister(Regs.REG_MODEM_CONFIG_1 | 0xfe));
    }

    public sbyte PacketRssi()
    {
        return (sbyte)(ReadRegister(Regs.REG_PKT_RSSI_VALUE) - (_frequency < 868000000 ? 164 : 157));
    }

    public byte PacketSnr()
    {
        return (byte)((sbyte)ReadRegister(Regs.REG_PKT_SNR_VALUE) >> 2);
    }

    private byte GetSpreadingFactor()
    {
        return (byte)(ReadRegister(Regs.REG_MODEM_CONFIG_2) >> 4);
    }

    private uint GetSignalBandwidth()
    {
        byte bw = (byte)(ReadRegister(Regs.REG_MODEM_CONFIG_1) >> 4);

        return bw switch
        {
            0 => 7800,
            1 => 10400,
            2 => 15600,
            3 => 20800,
            4 => 31250,
            5 => 41700,
            6 => 62500,
            7 => 125000,
            8 => 250000,
            _ => 500000
        };
    }

    private int PacketFrequencyError()
    {
        int freqError = (ReadRegister(Regs.REG_FREQ_ERROR_MSB) & 0b111);
        freqError = (freqError << 8) + ReadRegister(Regs.REG_FREQ_ERROR_MID);
        freqError = (freqError << 8) + ReadRegister(Regs.REG_FREQ_ERROR_LSB);

        if ((ReadRegister(Regs.REG_FREQ_ERROR_MSB) & 0b1000) != 0)
            freqError -= 524288;

        const float fXtal = 32000000.0f;
        float fError = ((freqError * (1 << 24)) / fXtal) * (GetSignalBandwidth() / 500000.0f);

        return (int)fError;
    }

    public byte Write(byte[] buffer, byte size)
    {
        byte currentLength = ReadRegister(Regs.REG_PAYLOAD_LENGTH);

        if (currentLength + size > Regs.MAX_PKT_LENGTH)
            size = (byte)(Regs.MAX_PKT_LENGTH - currentLength);

        for (int i = 0; i < size; i++)
            WriteRegister(Regs.REG_FIFO, buffer[i]);

        WriteRegister(Regs.REG_PAYLOAD_LENGTH, (byte)(currentLength + size));

        return size;
    }

    public sbyte Available()
    {
        return (sbyte)(ReadRegister(Regs.REG_RX_NB_BYTES) - packetIndex);
    }

    public sbyte Read()
    {
        if (Available() <= 0)
            return -1;

        packetIndex++;
        return (sbyte)ReadRegister(Regs.REG_FIFO);
    }

    public sbyte Peek()
    {
        if (Available() <= 0)
            return -1;

        int currentAddress = ReadRegister(Regs.REG_FIFO_ADDR_PTR);
        byte b = ReadRegister(Regs.REG_FIFO);
        WriteRegister(Regs.REG_FIFO_ADDR_PTR, (byte)currentAddress);

        return (sbyte)b;
    }

    public void Receive(int size)
    {
        if (size > 0)
        {
            ImplicitHeaderMode();
            WriteRegister(Regs.REG_PAYLOAD_LENGTH, (byte)(size & 0xFF));
        }
        else
        {
            ExplicitHeaderMode();
        }

        WriteRegister(Regs.REG_OP_MODE, Regs.MODE_LONG_RANGE_MODE | Regs.MODE_RX_CONTINUOUS);
    }

    /// /////////////////////////////////
    public void SetTxPower(byte level)
    {
        var result = 2;
        result = level < 2 ? 2 : level;
        WriteRegister(Regs.REG_PA_CONFIG, (byte)(Regs.PA_BOOST | (result - 2)));
    }

    public void SetFrequency(uint freq)
    {
        _frequency = freq;
        ulong frf = ((ulong)freq << 19) / 32000000;
        WriteRegister(Regs.REG_FRF_MSB, (byte)(frf >> 16));
        WriteRegister(Regs.REG_FRF_MID, (byte)(frf >> 8));
        WriteRegister(Regs.REG_FRF_LSB, (byte)(frf >> 0));
    }

    public void Idle()
        => WriteRegister(Regs.REG_OP_MODE, Regs.MODE_LONG_RANGE_MODE | Regs.MODE_STDBY);

    private byte SingleTransfer(byte address, byte value)
    {
        cs.Write(PinValue.Low);

        Span<byte> writeBuffer = [address, value];
        Span<byte> readBuffer = stackalloc byte[2];
        _spi.TransferFullDuplex(writeBuffer, readBuffer);

        cs.Write(PinValue.High);

        return readBuffer[1]; // response for second byte (same as AVR)
    }

    private byte ReadRegister(byte address)
    {
        return SingleTransfer((byte)(address & 0x7F), 0x00);
    }

    private void WriteRegister(byte address, byte value)
    {
        SingleTransfer((byte)(address | 0x80), value);
    }
}
