using AtkOs.Core.Protocol;

namespace AtkOs.Core.Tests;

public class DpiCodecTests
{
    // Values read from a real R1 SE+ and shown as 1000 / 1600 / 3200 by the official app.
    [Theory]
    [InlineData(0x17, 1000)]
    [InlineData(0x25, 1600)]
    [InlineData(0x4B, 3200)]
    [InlineData(0x12, 800)]
    public void Table_decodes_real_values(byte raw, int dpi)
    {
        Assert.Equal((dpi, dpi), DpiCodec.Decode(new DpiSlot(raw, raw, 0), DpiEncoding.Table));
    }

    [Fact]
    public void Table_round_trips_every_supported_value()
    {
        Assert.Equal(200, DpiTable.Entries.Length);
        var values = DpiTable.Entries.Select(e => e.Dpi)
            .Concat(DpiTable.Entries.Where(e => e.CanDouble).Select(e => e.Dpi * 2));
        Assert.Equal(18000, values.Max());
        foreach (var dpi in values)
        {
            var slot = DpiCodec.Encode(dpi, DpiEncoding.Table);
            Assert.Equal((dpi, dpi), DpiCodec.Decode(slot, DpiEncoding.Table));
        }
    }

    [Fact]
    public void Step50_round_trips()
    {
        for (int dpi = 50; dpi <= 26000; dpi += 50)
            Assert.Equal((dpi, dpi), DpiCodec.Decode(DpiCodec.Encode(dpi, DpiEncoding.Step50), DpiEncoding.Step50));
    }

    [Fact]
    public void Step10_round_trips()
    {
        for (int dpi = 10; dpi <= 10000; dpi += 10)
            Assert.Equal((dpi, dpi), DpiCodec.Decode(DpiCodec.Encode(dpi, DpiEncoding.Step10), DpiEncoding.Step10));
        for (int dpi = 10050; dpi < 30000; dpi += 50)
            Assert.Equal((dpi, dpi), DpiCodec.Decode(DpiCodec.Encode(dpi, DpiEncoding.Step10), DpiEncoding.Step10));
    }

    [Theory]
    [InlineData(DpiEncoding.Table, 18000, 1010, 1000)]
    [InlineData(DpiEncoding.Table, 18000, 99999, 18000)]
    [InlineData(DpiEncoding.Step50, 26000, 1234, 1250)]
    [InlineData(DpiEncoding.Step10, 42000, 1234, 1230)]
    [InlineData(DpiEncoding.Step50, 26000, 10, 50)]
    public void Nearest_snaps_to_storable_value(DpiEncoding enc, int max, int input, int expected)
    {
        Assert.Equal(expected, DpiCodec.Nearest(input, enc, max));
    }
}
