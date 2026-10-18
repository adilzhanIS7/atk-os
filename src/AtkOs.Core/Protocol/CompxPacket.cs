namespace AtkOs.Core.Protocol;

/// <summary>
/// 16-byte command/response packet of Compx-based mice.
/// Sent as output report ID 8; the response arrives as an input report
/// whose first byte echoes the command ID.
///
///  [0]     command ID
///  [1]     status (0 = OK, in responses)
///  [2..3]  EEPROM address, big-endian
///  [4]     number of valid data bytes
///  [5..14] data (up to 10 bytes)
///  [15]    checksum
/// </summary>
public sealed class CompxPacket
{
    public const int Size = 16;
    public const byte ReportId = 8;
    public const int DataOffset = 5;
    public const int MaxData = 10;

    public byte[] Raw { get; }

    public CompxPacket(byte[] raw)
    {
        if (raw.Length < Size) throw new ArgumentException($"Packet must be {Size} bytes", nameof(raw));
        Raw = raw;
    }

    public CompxPacket(CompxCommand command, ushort address = 0, byte dataLength = 0)
    {
        Raw = new byte[Size];
        Raw[0] = (byte)command;
        Address = address;
        DataLength = dataLength;
    }

    public CompxCommand Command => (CompxCommand)Raw[0];
    public byte Status => Raw[1];

    public ushort Address
    {
        get => (ushort)(Raw[2] << 8 | Raw[3]);
        set { Raw[2] = (byte)(value >> 8); Raw[3] = (byte)value; }
    }

    public byte DataLength
    {
        get => Raw[4];
        set => Raw[4] = value;
    }

    public ReadOnlySpan<byte> Data => Raw.AsSpan(DataOffset, MaxData);

    public byte this[int dataIndex]
    {
        get => Raw[DataOffset + dataIndex];
        set => Raw[DataOffset + dataIndex] = value;
    }

    public void SetData(ReadOnlySpan<byte> data)
    {
        if (data.Length > MaxData) throw new ArgumentException("Too much data", nameof(data));
        data.CopyTo(Raw.AsSpan(DataOffset));
    }

    /// <summary>checksum = 0x55 - (reportId + sum(bytes 0..14)), mod 256.</summary>
    public static byte ComputeChecksum(ReadOnlySpan<byte> packet)
    {
        int sum = ReportId;
        for (int i = 0; i < 15; i++) sum += packet[i];
        return (byte)(0x55 - sum);
    }

    /// <summary>Returns the bytes to send (checksum filled in).</summary>
    public byte[] ToBytes()
    {
        Raw[15] = ComputeChecksum(Raw);
        return Raw;
    }

    public override string ToString() => Convert.ToHexString(Raw);
}
