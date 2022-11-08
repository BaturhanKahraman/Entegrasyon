using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Shared.Middlewares
{
    public class PerformanceMiddleware
    {
        private readonly RequestDelegate _next;
        private static Stopwatch Stopwatch=new Stopwatch();
        private const int PerformanceTime = 5;
        public PerformanceMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context,
            ILogger<PerformanceMiddleware> logger,IConfiguration configuration)
        {
            Stopwatch.Restart();
            await _next.Invoke(context);
            Stopwatch.Stop();
            int performanceTimeInSec=configuration.GetValue("PerformanceCounterInSecond", PerformanceTime);
            if (Stopwatch.Elapsed.Seconds> performanceTimeInSec)
                logger.LogError($"Cevap gelmesi {PerformanceTime} saniyeden fazla sürdü.");
            
        }
    }
}
