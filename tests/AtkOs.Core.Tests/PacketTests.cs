using AtkOs.Core.Protocol;

namespace AtkOs.Core.Tests;

/// <summary>Packets captured from a real Dragonfly R1 SE+ (report ID 8 + 16 bytes).</summary>
public class PacketTests
{
    [Theory]
    [InlineData("080300000000000000000000000000004A")] // GetWirelessMouseOnline
    [InlineData("081200000000000000000000000000003B")] // GetMouseVersion
    [InlineData("080800000006000000000000000000003F")] // GetEeprom addr 0, len 6
    [InlineData("080800002C080000000000000000000011")] // GetEeprom addr 44, len 8
    [InlineData("080700002C08FF0000560000FF56000068")] // SetEeprom: DPI 1 color = red
    public void Checksum_matches_real_requests(string captured)
    {
        var raw = Convert.FromHexString(captured)[1..];
        Assert.Equal(raw[15], CompxPacket.ComputeChecksum(raw));
    }

    [Fact]
    public void Builds_request_identical_to_real_capture()
    {
        var p = new CompxPacket(CompxCommand.GetEeprom, 44, 8);
        Assert.Equal("080800002C080000000000000000000011", "08" + Convert.ToHexString(p.ToBytes()));
    }

    [Fact]
    public void Address_is_big_endian()
    {
        var p = new CompxPacket(CompxCommand.GetEeprom, 0x0123, 2);
        Assert.Equal(0x01, p.Raw[2]);
        Assert.Equal(0x23, p.Raw[3]);
        Assert.Equal(0x0123, p.Address);
    }
}
