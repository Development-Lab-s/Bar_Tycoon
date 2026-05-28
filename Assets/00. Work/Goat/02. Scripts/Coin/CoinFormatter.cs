namespace _00._Work.Goat._02._Scripts.Coin
{
    public static class CoinFormatter
    {
        public static string Format(long value)
        {
            if (value < 1_000L)              return value.ToString();
            if (value < 1_000_000L)          return $"{value / 1_000f:F1}K";
            if (value < 1_000_000_000L)      return $"{value / 1_000_000f:F1}M";
            if (value < 1_000_000_000_000L)  return $"{value / 1_000_000_000f:F1}B";
            return $"{value / 1_000_000_000_000f:F1}T";
        }
    }
}
