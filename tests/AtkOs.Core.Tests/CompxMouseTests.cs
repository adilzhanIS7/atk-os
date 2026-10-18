using AtkOs.Core.Protocol;

namespace AtkOs.Core.Tests;

public class CompxMouseTests
{
    private readonly FakeMouse _fake = new();
    private readonly CompxMouse _mouse;

    public CompxMouseTests()
    {
        // Real values from a Dragonfly R1 SE+: 1000 Hz, 2 DPI levels, active #1, LOD 1,
        // DPI 1000 / 1600, colors green / blue, debounce 4 ms.
        Load(0, "0154025300550000");
        Load(10, "0154");
        Load(12, "17170027252500 0B".Replace(" ", ""));
        Load(44, "00FF00560000FF56");
        Load(169, "04510055064F00550055");
        _mouse = new CompxMouse(_fake);
    }

    private void Load(int address, string hex) => Convert.FromHexString(hex).CopyTo(_fake.Eeprom, address);

    private string Bytes(int address, int length) => Convert.ToHexString(_fake.Eeprom, address, length);

    [Fact]
    public void Reads_identity_and_settings()
    {
        Assert.True(_mouse.IsOnline());
        var id = _mouse.Handshake();
        Assert.Equal("Dragonfly R1 SE+", id.Model.Title);
        Assert.Equal(DpiEncoding.Table, id.DpiEncoding);

        var rr = _mouse.GetReportRateDpi();
        Assert.Equal(ReportRate.Hz1000, rr.ReportRate);
        Assert.Equal(2, rr.DpiSlotCount);

        var slots = _mouse.GetDpiSlots();
        Assert.Equal(1000, DpiCodec.Decode(slots[0], id.DpiEncoding).X);
        Assert.Equal(1600, DpiCodec.Decode(slots[1], id.DpiEncoding).X);
        Assert.Equal(4, _mouse.GetPerformance().Debounce);
    }

    [Fact]
    public void Color_write_sends_exactly_what_the_real_mouse_accepted()
    {
        _mouse.SetDpiColor(0, new RgbColor(0xFF, 0, 0));

        Assert.Contains("080700002C08FF0000560000FF56000068", _fake.Sent);
        Assert.Equal("FF0000560000FF56", Bytes(44, 8)); // DPI 2 color kept
    }

    [Fact]
    public void Polling_rate_changes_only_its_pair()
    {
        _mouse.SetReportRate(ReportRate.Hz500);
        Assert.Equal("025302530055", Bytes(0, 6));
    }

    [Fact]
    public void Dpi_slot_write_keeps_neighbour()
    {
        _mouse.SetDpiSlot(0, DpiCodec.Encode(800, DpiEncoding.Table));
        Assert.Equal("1212003125250 00B".Replace(" ", ""), Bytes(12, 8));
    }

    [Fact]
    public void Debounce_and_toggles()
    {
        _mouse.SetDebounce(8);
        _mouse.SetToggle(PerformanceToggle.MotionSync, true);
        _mouse.SetToggle(PerformanceToggle.RippleControl, true);
        Assert.Equal("084D0154064F00550154", Bytes(169, 10));
    }

    [Fact]
    public void Rejects_invalid_values_without_sending()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _mouse.SetDebounce(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => _mouse.SetLiftOffDistance(9));
        Assert.Throws<ArgumentOutOfRangeException>(() => _mouse.SetDpiColor(8, new RgbColor(0, 0, 0)));
        Assert.Empty(_fake.Sent);
    }

    [Fact]
    public void Write_that_was_not_stored_is_detected()
    {
        _fake.IgnoreWrites = true;
        var e = Assert.Throws<InvalidOperationException>(() => _mouse.SetReportRate(ReportRate.Hz500));
        Assert.Contains("Verify failed", e.Message);
    }

    [Fact]
    public void Error_status_is_reported()
    {
        _fake.FailStatus = 1;
        Assert.Throws<InvalidOperationException>(() => _mouse.GetBattery());
    }
}
