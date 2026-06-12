using Entegrasyon.Business.Helpers;

namespace Entegrasyon.UnitTest.Reports;

public class QuarterHelperTests
{
    [Theory]
    [InlineData(2026, 2, 15, 2026, 1, 1, 2026, 3, 31)]   // Q1
    [InlineData(2026, 5, 1, 2026, 4, 1, 2026, 6, 30)]    // Q2
    [InlineData(2026, 9, 30, 2026, 7, 1, 2026, 9, 30)]   // Q3
    [InlineData(2026, 11, 20, 2026, 10, 1, 2026, 12, 31)]// Q4
    public void GetQuarterRange_ReturnsCalendarQuarterBounds(
        int y, int m, int d, int sy, int sm, int sd, int ey, int em, int ed)
    {
        var (start, end) = QuarterHelper.GetQuarterRange(new DateOnly(y, m, d));
        Assert.Equal(new DateOnly(sy, sm, sd), start);
        Assert.Equal(new DateOnly(ey, em, ed), end);
    }

    [Fact]
    public void GetPreviousQuarterRange_FromQ1_ReturnsPreviousYearQ4()
    {
        var (start, end) = QuarterHelper.GetPreviousQuarterRange(new DateOnly(2026, 2, 15));
        Assert.Equal(new DateOnly(2025, 10, 1), start);
        Assert.Equal(new DateOnly(2025, 12, 31), end);
    }
}
