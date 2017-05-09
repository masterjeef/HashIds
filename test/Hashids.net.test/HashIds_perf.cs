using System;
using Xunit;
using System.Diagnostics;

namespace HashidsNet.test
{
    public class HashIds_perf
    {
        static Random random = new Random();

        [Fact]
        void Encode_single()
        {
            var hashids = new HashIds();
            var stopWatch = Stopwatch.StartNew();
            for (var i = 1; i < 10001; i++)
            {
                hashids.Encode(i);
            }
            stopWatch.Stop();
            Trace.WriteLine($"10 000 encodes: {stopWatch.ElapsedMilliseconds}");
        }
    }
}
