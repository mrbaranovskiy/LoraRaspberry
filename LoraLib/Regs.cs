public static class Regs
{
    // registers
    public const byte REG_FIFO                    = 0x00;
    public const byte REG_OP_MODE                 = 0x01;
    public const byte REG_FRF_MSB                 = 0x06;
    public const byte REG_FRF_MID                 = 0x07;
    public const byte REG_FRF_LSB                 = 0x08;
    public const byte REG_PA_CONFIG               = 0x09;
    public const byte REG_LNA                     = 0x0C;
    public const byte REG_FIFO_ADDR_PTR           = 0x0D;
    public const byte REG_FIFO_TX_BASE_ADDR       = 0x0E;
    public const byte REG_FIFO_RX_BASE_ADDR       = 0x0F;
    public const byte REG_FIFO_RX_CURRENT_ADDR    = 0x10;
    public const byte REG_IRQ_FLAGS               = 0x12;
    public const byte REG_RX_NB_BYTES             = 0x13;
    public const byte REG_PKT_SNR_VALUE           = 0x19;
    public const byte REG_PKT_RSSI_VALUE          = 0x1A;
    public const byte REG_MODEM_CONFIG_1          = 0x1D;
    public const byte REG_MODEM_CONFIG_2          = 0x1E;
    public const byte REG_PREAMBLE_MSB            = 0x20;
    public const byte REG_PREAMBLE_LSB            = 0x21;
    public const byte REG_PAYLOAD_LENGTH          = 0x22;
    public const byte REG_MODEM_CONFIG_3          = 0x26;
    public const byte REG_FREQ_ERROR_MSB          = 0x28;
    public const byte REG_FREQ_ERROR_MID          = 0x29;
    public const byte REG_FREQ_ERROR_LSB          = 0x2A;
    public const byte REG_RSSI_WIDEBAND           = 0x2C;
    public const byte REG_DETECTION_OPTIMIZE      = 0x31;
    public const byte REG_DETECTION_THRESHOLD     = 0x37;
    public const byte REG_SYNC_WORD               = 0x39;
    public const byte REG_DIO_MAPPING_1           = 0x40;
    public const byte REG_VERSION                 = 0x42;

    // modes
    public const byte MODE_LONG_RANGE_MODE        = 0x80;
    public const byte MODE_SLEEP                  = 0x00;
    public const byte MODE_STDBY                  = 0x01;
    public const byte MODE_TX                     = 0x03;
    public const byte MODE_RX_CONTINUOUS          = 0x05;
    public const byte MODE_RX_SINGLE              = 0x06;

    // PA config
    public const byte PA_BOOST                    = 0x80;

    // IRQ masks
    public const byte IRQ_TX_DONE_MASK            = 0x08;
    public const byte IRQ_PAYLOAD_CRC_ERROR_MASK  = 0x20;
    public const byte IRQ_RX_DONE_MASK            = 0x40;

    // package
    public const byte MAX_PKT_LENGTH              = 255;
}
