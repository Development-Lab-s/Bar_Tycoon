using System.Threading;

namespace BBJ
{
    public static class CancellationExtensions
    {
        public static void CancelAndDispose(this CancellationTokenSource cts)
        {
            cts?.Cancel();
            cts?.Dispose();
        }
    }
}
