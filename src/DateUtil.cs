namespace Ledger;

public static class DateUtil
{
    // public static string NowIso() =>
    //     DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

    public static long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}