using System.Diagnostics;

namespace GiftCertificateService.Services
{
    public static class StopwatchExtensions
    {
        public static void StartMeasure(this Stopwatch stopwatch)
        {
            stopwatch.Reset();
            stopwatch.Start();
        }

        public static long EndMeasure(this Stopwatch stopwatch)
        {
            if (stopwatch.IsRunning)
                stopwatch.Stop();

            return stopwatch.ElapsedMilliseconds;
        }
    }
}
